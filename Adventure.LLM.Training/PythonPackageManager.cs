using System.Diagnostics;
using System.Runtime.InteropServices;

public class PythonPackageManager
{
	private readonly string _pythonHome;
	private readonly string _pythonExe;
	private readonly string _pipExe;
	private readonly string _cacheDir;
	private readonly bool _isWindows;
	private readonly bool _isLinux;

	public PythonPackageManager(string pythonHome)
	{
		_pythonHome = pythonHome;
		_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

		if (_isWindows)
		{
			_pythonExe = Path.Combine(_pythonHome, "python.exe");
			_pipExe = Path.Combine(_pythonHome, "Scripts", "pip.exe");
		}
		else if (_isLinux)
		{
			_pythonExe = Path.Combine(_pythonHome, "bin", "python3");
			_pipExe = Path.Combine(_pythonHome, "bin", "pip3");
		}

		_cacheDir = Path.Combine(_pythonHome, "pip-cache");
		Directory.CreateDirectory(_cacheDir);
	}

	public async Task<bool> IsPackageInstalled(string packageName)
	{
		try
		{
			var output = await RunPipCommand($"show {packageName}");
			return !string.IsNullOrEmpty(output);
		}
		catch
		{
			return false;
		}
	}

	public async Task InstallPackageWithDependencies(string packageSpec)
	{
		// Use --prefer-binary to avoid compilation on Linux when possible
		string args = $"install {packageSpec} --cache-dir \"{_cacheDir}\"";

		if (_isLinux)
		{
			args += " --prefer-binary";
		}

		await RunPipCommand(args);
	}

	public async Task InstallFromRequirements(string requirementsPath)
	{
		string args = $"install -r \"{requirementsPath}\" --cache-dir \"{_cacheDir}\"";

		if (_isLinux)
		{
			args += " --prefer-binary";
		}

		await RunPipCommand(args);
	}

	private async Task<string> RunPipCommand(string arguments)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = File.Exists(_pipExe) ? _pipExe : _pythonExe,
			Arguments = File.Exists(_pipExe) ? arguments : $"-m pip {arguments}",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			WorkingDirectory = _pythonHome
		};

		using var process = Process.Start(startInfo);
		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			throw new Exception($"Pip command failed: {error}");
		}

		return output;
	}
}