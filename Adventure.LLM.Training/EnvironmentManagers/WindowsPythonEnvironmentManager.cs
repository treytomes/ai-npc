namespace Adventure.LLM.Training.EnvironmentManagers;

internal class WindowsPythonEnvironmentManager(string appName)
	: PythonEnvironmentManager(Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		appName,
		"Python"
	))
{
	protected override void SetEnvironmentPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome))
		{
			throw new NullReferenceException("Python home isn't set.");
		}

		var majorMinor = string.Join("", _pythonVersion.Split('.').Take(2));
		_pythonDll = Path.Combine(_pythonHome, $"python{majorMinor}.dll");
		_pipPath = Path.Combine(_pythonHome, "Scripts", "pip.exe");
	}

	protected override IEnumerable<string> GetPythonPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome))
		{
			throw new NullReferenceException("Python home isn't set.");
		}

		var pythonPaths = new List<string>();

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

		return pythonPaths;
	}

	protected override void VerifyPythonPaths()
	{
		if (string.IsNullOrWhiteSpace(_pythonHome)) throw new NullReferenceException("Python home isn't set.");

		// Verify that the standard library exists.
		string libPath = Path.Combine(_pythonHome, "Lib");
		if (!Directory.Exists(libPath))
		{
			throw new DirectoryNotFoundException($"Python standard library not found at: {libPath}");
		}

		// Check for encodings module specifically.
		string encodingsPath = Path.Combine(libPath, "encodings");
		if (!Directory.Exists(encodingsPath))
		{
			throw new DirectoryNotFoundException($"Python encodings module not found at: {encodingsPath}");
		}
	}

	public override bool VerifyPythonLibrary()
	{
		try
		{
			return File.Exists(_pythonDll);
		}
		catch (Exception ex)
		{
			ReportOutput($"Failed to load Python library: {ex.Message}");
			ReportOutput($"Python DLL path: {_pythonDll}");
			return false;
		}
	}
}
