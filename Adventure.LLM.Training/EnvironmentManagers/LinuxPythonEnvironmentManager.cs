using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Adventure.LLM.Training.EnvironmentManagers;

internal class LinuxPythonEnvironmentManager(string appName)
	: PythonEnvironmentManager(Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		$".{appName.ToLower()}",
		"python"
	))
{
	protected override void SetEnvironmentPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome))
		{
			throw new NullReferenceException("Python home isn't set.");
		}

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

	protected override IEnumerable<string> GetPythonPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome))
		{
			throw new NullReferenceException("Python home isn't set.");
		}

		var pythonPaths = new List<string>();

		var versionParts = _pythonVersion.Split('.');
		var majorMinor = $"{versionParts[0]}.{versionParts[1]}";

		// Add the base directory.
		pythonPaths.Add(_pythonHome);

		// Add standard library paths.
		var libPython = Path.Combine(_pythonHome, "lib", $"python{majorMinor}");
		pythonPaths.Add(libPython);
		pythonPaths.Add(Path.Combine(libPython, "lib-dynload"));
		pythonPaths.Add(Path.Combine(libPython, "site-packages"));

		// Also check for the standard system paths in case some modules are there.
		var systemLibPython = $"/usr/lib/python{majorMinor}";
		if (Directory.Exists(systemLibPython))
		{
			pythonPaths.Add(systemLibPython);
			pythonPaths.Add(Path.Combine(systemLibPython, "lib-dynload"));
		}

		return pythonPaths;
	}

	protected override void ConfigureEnvironment()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome))
		{
			throw new NullReferenceException("Python home isn't set.");
		}

		// Set LD_LIBRARY_PATH to include Python's lib directory.
		var libPath = Path.Combine(_pythonHome, "lib");
		var currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";

		var ldPaths = currentLdPath.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
		if (!ldPaths.Contains(libPath))
		{
			ldPaths.Insert(0, libPath);
		}

		// Also add the directory containing the Python shared library.
		var pythonDllDir = Path.GetDirectoryName(_pythonDll);
		if (!string.IsNullOrWhiteSpace(pythonDllDir) && !ldPaths.Contains(pythonDllDir))
		{
			ldPaths.Insert(0, pythonDllDir);
		}

		Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", string.Join(":", ldPaths));
	}

	protected override void VerifyPythonPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome)) throw new NullReferenceException("Python home isn't set.");

		// Verify that the standard library exists.
		var versionParts = _pythonVersion.Split('.');
		var majorMinor = $"{versionParts[0]}.{versionParts[1]}";
		var libPath = Path.Combine(_pythonHome, "lib", $"python{majorMinor}");

		if (!Directory.Exists(libPath))
		{
			throw new DirectoryNotFoundException($"Python standard library not found at: {libPath}");
		}

		// Check for encodings module specifically.
		string encodingsPath = Path.Combine(libPath, "encodings");
		if (!Directory.Exists(encodingsPath))
		{
			// Try to find it in the system paths.
			string systemEncodingsPath = Path.Combine($"/usr/lib/python{majorMinor}", "encodings");
			if (!Directory.Exists(systemEncodingsPath))
			{
				throw new DirectoryNotFoundException($"Python encodings module not found at: {encodingsPath} or {systemEncodingsPath}");
			}
		}
	}

	protected override ProcessStartInfo ConfigurePackageInstaller(ProcessStartInfo startInfo, string packageName)
	{
		if (string.IsNullOrWhiteSpace(_pythonHome)) throw new NullReferenceException("Python home isn't set.");

		// Ensure LD_LIBRARY_PATH is set for the subprocess
		var libPath = Path.Combine(_pythonHome, "lib");
		var pythonDllDir = Path.GetDirectoryName(_pythonDll);
		var ldPath = $"{pythonDllDir}:{libPath}";

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

		return startInfo;
	}

	public override bool VerifyPythonLibrary()
	{
		try
		{
			// First check if file exists.
			if (!File.Exists(_pythonDll))
			{
				ReportOutput($"Python DLL file not found: {_pythonDll}");
				return false;
			}

			// Try to load the library.
			var handle = NativeLibrary.Load(_pythonDll);
			NativeLibrary.Free(handle);
			return true;
		}
		catch (Exception ex)
		{
			ReportOutput($"Failed to load Python library: {ex.Message}");
			ReportOutput($"Python DLL path: {_pythonDll}");
			ReportOutput($"Make sure LD_LIBRARY_PATH includes: {Path.GetDirectoryName(_pythonDll)}");
			return false;
		}
	}
}
