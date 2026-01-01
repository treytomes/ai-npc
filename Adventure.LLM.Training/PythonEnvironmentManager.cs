using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Python.Runtime;

namespace Adventure.LLM.Training;

internal class PythonEnvironmentManager : IDisposable
{
	private readonly Subject<OutputReceivedEventArgs> _outputReceivedSubject = new();

	private readonly string _appDataPath;
	private readonly string _pythonVersion = "3.11.7";
	private string? _pythonHome;
	private string? _pythonDll;
	private string? _pipPath;
	private readonly bool _isWindows;
	private readonly bool _isLinux;
	private static bool _isConfigured = false;
	private bool _disposedValue;

	public PythonEnvironmentManager(string appName)
	{
		_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

		if (_isWindows)
		{
			_appDataPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				appName,
				"Python"
			);
		}
		else if (_isLinux)
		{
			_appDataPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				$".{appName.ToLower()}",
				"python"
			);
		}
		else
		{
			throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}.");
		}
	}

	public IObservable<OutputReceivedEventArgs> WhenOutputReceived => _outputReceivedSubject;

	public async Task<bool> SetupEnvironmentAsync()
	{
		try
		{
			// Install Python
			var installer = new PythonInstaller();

			// Forward output from installer
			installer.WhenOutputReceived.Subscribe(_outputReceivedSubject.OnNext);

			_pythonHome = await installer.InstallPythonAsync();

			// Set paths based on OS
			if (_isWindows)
			{
				var majorMinor = string.Join("", _pythonVersion.Split('.').Take(2));
				_pythonDll = Path.Combine(_pythonHome, $"python{majorMinor}.dll");
				_pipPath = Path.Combine(_pythonHome, "Scripts", "pip.exe");
			}
			else if (_isLinux)
			{
				string[] versionParts = _pythonVersion.Split('.');
				string majorMinor = $"{versionParts[0]}.{versionParts[1]}";

				string libDir = Path.Combine(_pythonHome, "lib");
				var possibleLibNames = new[]
				{
					$"libpython{majorMinor}.so.1.0",
					$"libpython{majorMinor}.so",
					$"libpython{versionParts[0]}.so"
				};

				foreach (var libName in possibleLibNames)
				{
					var candidates = Directory.GetFiles(libDir, libName, SearchOption.AllDirectories);
					if (candidates.Any())
					{
						_pythonDll = candidates.First();
						break;
					}
				}

				if (string.IsNullOrEmpty(_pythonDll) || !File.Exists(_pythonDll))
				{
					throw new FileNotFoundException($"Could not find Python shared library in {libDir}");
				}

				_pipPath = Path.Combine(_pythonHome, "bin", "pip3");
			}

			if (!File.Exists(_pythonDll))
			{
				throw new FileNotFoundException($"Python library not found at: {_pythonDll}");
			}

			// Configure Python.NET BEFORE installing packages
			// This is important - we need to set Runtime.PythonDLL before any Python operations
			ConfigurePythonNet();

			// Install required packages
			await InstallRequiredPackages();

			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error setting up Python environment: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	private void ConfigurePythonNet()
	{
		// Only configure once
		if (_isConfigured)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(_pythonHome)) throw new NullReferenceException("Python home isn't set.");

		try
		{
			// CRITICAL: Set the Python DLL first, before any other Python.NET operations
			Runtime.PythonDLL = _pythonDll;

			// Verify the DLL can be loaded
			if (!VerifyPythonLibrary())
			{
				throw new Exception($"Failed to verify Python library at: {_pythonDll}");
			}

			// Set Python home - this is critical for finding the standard library
			PythonEngine.PythonHome = _pythonHome;

			// Build Python path with all necessary directories
			var pythonPaths = new List<string>();

			if (_isWindows)
			{
				// Add the base directory
				pythonPaths.Add(_pythonHome);

				// Add standard library paths
				pythonPaths.Add(Path.Combine(_pythonHome, "Lib"));
				pythonPaths.Add(Path.Combine(_pythonHome, "DLLs"));
				pythonPaths.Add(Path.Combine(_pythonHome, "Lib", "site-packages"));

				// Add the python zip file if it exists
				string pythonZip = Path.Combine(_pythonHome, $"python{_pythonVersion.Replace(".", "")}.zip");
				if (File.Exists(pythonZip))
				{
					pythonPaths.Add(pythonZip);
				}
			}
			else if (_isLinux)
			{
				string[] versionParts = _pythonVersion.Split('.');
				string majorMinor = $"{versionParts[0]}.{versionParts[1]}";

				// Add the base directory
				pythonPaths.Add(_pythonHome);

				// Add standard library paths
				string libPython = Path.Combine(_pythonHome, "lib", $"python{majorMinor}");
				pythonPaths.Add(libPython);
				pythonPaths.Add(Path.Combine(libPython, "lib-dynload"));
				pythonPaths.Add(Path.Combine(libPython, "site-packages"));

				// Also check for the standard system paths in case some modules are there
				string systemLibPython = $"/usr/lib/python{majorMinor}";
				if (Directory.Exists(systemLibPython))
				{
					pythonPaths.Add(systemLibPython);
					pythonPaths.Add(Path.Combine(systemLibPython, "lib-dynload"));
				}
			}

			// Set the Python path
			string pythonPath = string.Join(Path.PathSeparator.ToString(), pythonPaths);
			PythonEngine.PythonPath = pythonPath;

			// Set environment variables
			Environment.SetEnvironmentVariable("PYTHONHOME", _pythonHome);
			Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);

			// Don't isolate Python - we want it to use our custom paths
			Environment.SetEnvironmentVariable("PYTHONNOUSERSITE", "1");

			if (_isLinux)
			{
				// Set LD_LIBRARY_PATH to include Python's lib directory
				string libPath = Path.Combine(_pythonHome, "lib");
				string currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";

				var ldPaths = currentLdPath.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
				if (!ldPaths.Contains(libPath))
				{
					ldPaths.Insert(0, libPath);
				}

				// Also add the directory containing the Python shared library
				var pythonDllDir = Path.GetDirectoryName(_pythonDll);
				if (!string.IsNullOrWhiteSpace(pythonDllDir) && !ldPaths.Contains(pythonDllDir))
				{
					ldPaths.Insert(0, pythonDllDir);
				}

				Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", string.Join(":", ldPaths));
			}

			// Debug output
			ReportOutput($"Python DLL: {_pythonDll}");
			ReportOutput($"Python Home: {_pythonHome}");
			ReportOutput($"Python Path: {pythonPath}");

			_isConfigured = true;
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to configure Python.NET: {ex.Message}", ex);
		}
	}

	private async Task InstallRequiredPackages()
	{
		var packages = new List<string>
		{
			"numpy",
			"torch",
			"transformers",
			"peft",
			"datasets",
			"accelerate"
		};

		foreach (var package in packages)
		{
			await InstallPackage(package);
		}
	}

	private async Task InstallPackage(string packageName)
	{
		ReportOutput($"Installing {packageName}...");

		if (string.IsNullOrWhiteSpace(_pythonHome)) throw new NullReferenceException("Python home isn't set.");

		var startInfo = new ProcessStartInfo
		{
			FileName = _pipPath,
			Arguments = $"install {packageName}",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			WorkingDirectory = _pythonHome
		};

		// Set environment variables for the pip process
		startInfo.Environment["PYTHONHOME"] = _pythonHome;
		startInfo.Environment["PYTHONPATH"] = PythonEngine.PythonPath;

		if (_isLinux)
		{
			// Ensure LD_LIBRARY_PATH is set for the subprocess
			string libPath = Path.Combine(_pythonHome, "lib");
			var pythonDllDir = Path.GetDirectoryName(_pythonDll);
			string ldPath = $"{pythonDllDir}:{libPath}";

			var existingLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
			if (!string.IsNullOrEmpty(existingLdPath))
			{
				ldPath = $"{ldPath}:{existingLdPath}";
			}

			startInfo.Environment["LD_LIBRARY_PATH"] = ldPath;

			if (!File.Exists(_pipPath))
			{
				startInfo.FileName = Path.Combine(_pythonHome, "bin", "python3");
				startInfo.Arguments = $"-m pip install {packageName}";
			}
		}

		using var process = Process.Start(startInfo) ?? throw new NullReferenceException("Unable to run 'pip'.");

		// Stream output in real-time
		var outputTask = Task.Run(async () =>
		{
			while (!process.StandardOutput.EndOfStream)
			{
				var line = await process.StandardOutput.ReadLineAsync();
				if (!string.IsNullOrWhiteSpace(line))
				{
					ReportOutput($"  {line}");
				}
			}
		});

		var errorTask = Task.Run(async () =>
		{
			while (!process.StandardError.EndOfStream)
			{
				var line = await process.StandardError.ReadLineAsync();
				if (!string.IsNullOrWhiteSpace(line))
				{
					ReportOutput($"  [ERROR] {line}");
				}
			}
		});

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			ReportOutput($"Warning: Failed to install {packageName}");
		}
		else
		{
			ReportOutput($"Successfully installed {packageName}");
		}
	}

	public void Initialize()
	{
		if (!PythonEngine.IsInitialized)
		{
			try
			{
				// Make sure we're configured first
				if (!_isConfigured)
				{
					throw new InvalidOperationException("Python environment not configured. Call SetupEnvironmentAsync first.");
				}

				// Verify paths before initialization
				VerifyPythonPaths();

				// Double-check that Runtime.PythonDLL is set
				if (string.IsNullOrEmpty(Runtime.PythonDLL))
				{
					throw new InvalidOperationException($"Runtime.PythonDLL is not set. Expected: {_pythonDll}");
				}

				PythonEngine.Initialize();

				using (Py.GIL())
				{
					// Test that we can import basic modules
					dynamic sys = Py.Import("sys");
					Console.WriteLine($"Python version: {sys.version}");
					Console.WriteLine($"Python executable: {sys.executable}");

					// Verify encodings module is accessible
					try
					{
						dynamic encodings = Py.Import("encodings");
						Console.WriteLine("Successfully imported encodings module");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Warning: Could not import encodings module: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Python initialization error: {ex.Message}");
				Console.WriteLine($"Python DLL: {Runtime.PythonDLL}");
				Console.WriteLine($"Python Home: {PythonEngine.PythonHome}");
				Console.WriteLine($"Python Path: {PythonEngine.PythonPath}");
				throw;
			}
		}
	}

	private void VerifyPythonPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome)) throw new NullReferenceException("Python home isn't set.");

		// Verify that the standard library exists
		if (_isWindows)
		{
			string libPath = Path.Combine(_pythonHome, "Lib");
			if (!Directory.Exists(libPath))
			{
				throw new DirectoryNotFoundException($"Python standard library not found at: {libPath}");
			}

			// Check for encodings module specifically
			string encodingsPath = Path.Combine(libPath, "encodings");
			if (!Directory.Exists(encodingsPath))
			{
				throw new DirectoryNotFoundException($"Python encodings module not found at: {encodingsPath}");
			}
		}
		else if (_isLinux)
		{
			string[] versionParts = _pythonVersion.Split('.');
			string majorMinor = $"{versionParts[0]}.{versionParts[1]}";
			string libPath = Path.Combine(_pythonHome, "lib", $"python{majorMinor}");

			if (!Directory.Exists(libPath))
			{
				throw new DirectoryNotFoundException($"Python standard library not found at: {libPath}");
			}

			// Check for encodings module specifically
			string encodingsPath = Path.Combine(libPath, "encodings");
			if (!Directory.Exists(encodingsPath))
			{
				// Try to find it in the system paths
				string systemEncodingsPath = Path.Combine($"/usr/lib/python{majorMinor}", "encodings");
				if (!Directory.Exists(systemEncodingsPath))
				{
					throw new DirectoryNotFoundException($"Python encodings module not found at: {encodingsPath} or {systemEncodingsPath}");
				}
			}
		}
	}

	public void Shutdown()
	{
		try
		{
			if (PythonEngine.IsInitialized)
			{
				// Try to clean up any remaining Python objects
				GC.Collect();
				GC.WaitForPendingFinalizers();

				PythonEngine.Shutdown();
			}
		}
		catch (Exception ex)
		{
			// Log but don't throw - shutdown errors are often not critical
			Console.WriteLine($"Warning during Python shutdown: {ex.Message}");
		}
	}

	public string? GetPythonHome()
	{
		return _pythonHome;
	}

	public bool VerifyPythonLibrary()
	{
		try
		{
			if (_isLinux)
			{
				// First check if file exists
				if (!File.Exists(_pythonDll))
				{
					Console.WriteLine($"Python DLL file not found: {_pythonDll}");
					return false;
				}

				// Try to load the library
				var handle = NativeLibrary.Load(_pythonDll);
				NativeLibrary.Free(handle);
				return true;
			}
			else
			{
				return File.Exists(_pythonDll);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to load Python library: {ex.Message}");
			Console.WriteLine($"Python DLL path: {_pythonDll}");
			if (_isLinux)
			{
				Console.WriteLine($"Make sure LD_LIBRARY_PATH includes: {Path.GetDirectoryName(_pythonDll)}");
			}
			return false;
		}
	}

	private void ReportOutput(string message)
	{
		_outputReceivedSubject.OnNext(new(message));
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_outputReceivedSubject.OnCompleted();
				_outputReceivedSubject.Dispose();
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			_disposedValue = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~PythonEnvironmentManager()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}