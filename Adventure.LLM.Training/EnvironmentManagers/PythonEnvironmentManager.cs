using System.Diagnostics;
using System.Reactive.Subjects;
using Python.Runtime;

namespace Adventure.LLM.Training.EnvironmentManagers;

internal abstract class PythonEnvironmentManager(string appDataPath) : IPythonEnvironmentManager
{
	#region Fields

	private readonly Subject<OutputReceivedEventArgs> _outputReceivedSubject = new();

	private readonly string _appDataPath = appDataPath;
	protected readonly string _pythonVersion = "3.11.7";
	protected string? _pythonHome;
	protected string? _pythonDll;
	protected string? _pipPath;
	private static bool _isConfigured = false;
	private bool _disposedValue;

	#endregion

	#region Properties

	public IObservable<OutputReceivedEventArgs> WhenOutputReceived => _outputReceivedSubject;

	#endregion

	#region Methods

	protected abstract void SetEnvironmentPaths();

	public async Task<bool> SetupEnvironmentAsync()
	{
		try
		{
			// Install Python
			var installer = new PythonFactory().GetInstaller(new ConsolePasswordTextReader());

			// Forward output from installer
			installer.WhenOutputReceived.Subscribe(_outputReceivedSubject.OnNext);

			_pythonHome = await installer.InstallPythonAsync();

			// Set paths based on OS.
			SetEnvironmentPaths();

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
			ReportOutput($"Error setting up Python environment: {ex.Message}");
			ReportOutput($"Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	protected abstract IEnumerable<string> GetPythonPaths();

	protected virtual void ConfigureEnvironment()
	{
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
			var pythonPaths = GetPythonPaths().ToList();

			// Set the Python path
			string pythonPath = string.Join(Path.PathSeparator.ToString(), pythonPaths);
			PythonEngine.PythonPath = pythonPath;

			// Set environment variables
			Environment.SetEnvironmentVariable("PYTHONHOME", _pythonHome);
			Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);

			// Don't isolate Python - we want it to use our custom paths
			Environment.SetEnvironmentVariable("PYTHONNOUSERSITE", "1");

			ConfigureEnvironment();

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

	protected virtual ProcessStartInfo ConfigurePackageInstaller(ProcessStartInfo startInfo, string packageName)
	{
		return startInfo;
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

		startInfo = ConfigurePackageInstaller(startInfo, packageName);

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
					ReportOutput($"Python version: {sys.version}");
					ReportOutput($"Python executable: {sys.executable}");

					// Verify encodings module is accessible
					try
					{
						dynamic encodings = Py.Import("encodings");
						ReportOutput("Successfully imported encodings module");
					}
					catch (Exception ex)
					{
						ReportOutput($"Warning: Could not import encodings module: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				ReportOutput($"Python initialization error: {ex.Message}");
				ReportOutput($"Python DLL: {Runtime.PythonDLL}");
				ReportOutput($"Python Home: {PythonEngine.PythonHome}");
				ReportOutput($"Python Path: {PythonEngine.PythonPath}");
				throw;
			}
		}
	}

	protected abstract void VerifyPythonPaths();

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
			// Log but don't throw - shutdown errors are often not critical.
			ReportOutput($"Warning during Python shutdown: {ex.Message}");
		}
	}

	public string? GetPythonHome()
	{
		return _pythonHome;
	}

	public abstract bool VerifyPythonLibrary();

	protected void ReportOutput(string message)
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

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}