using System.Diagnostics;

namespace Adventure.LLM.Training.PackageManagers;

internal class PythonPackageManager : IPythonPackageManager
{
	#region Fields

	private readonly string _pythonHome;
	private readonly string _pythonExe;
	private readonly string _pipExe;
	private readonly string _cacheDir;

	#endregion

	#region Constructors

	protected PythonPackageManager(string pythonHome, string pythonExe, string pipExe)
	{
		_pythonHome = pythonHome;
		_pythonExe = pythonExe;
		_pipExe = pipExe;
		_cacheDir = Path.Combine(_pythonHome, "pip-cache");
		Directory.CreateDirectory(_cacheDir);
	}

	#endregion

	#region Methods

	public async Task<bool> IsPackageInstalledAsync(string packageName)
	{
		try
		{
			var output = await RunPipCommandAsync($"show {packageName}");
			return !string.IsNullOrEmpty(output);
		}
		catch
		{
			return false;
		}
	}

	public async Task InstallPackageWithDependenciesAsync(string packageSpec)
	{
		// Use --prefer-binary to avoid compilation on Linux when possible
		var args = string.Join(" ",
			$"install {packageSpec} --cache-dir \"{_cacheDir}\"",
			GetInstallArgs()
		);
		await RunPipCommandAsync(args);
	}

	public async Task InstallFromRequirementsAsync(string requirementsPath)
	{
		var args = string.Join(" ",
			$"install -r \"{requirementsPath}\" --cache-dir \"{_cacheDir}\"",
			GetInstallArgs()
		);
		await RunPipCommandAsync(args);
	}

	protected virtual string GetInstallArgs() => string.Empty;

	private async Task<string> RunPipCommandAsync(string arguments)
	{
		var filename = File.Exists(_pipExe) ? _pipExe : _pythonExe;
		var startInfo = new ProcessStartInfo
		{
			FileName = filename,
			Arguments = File.Exists(_pipExe) ? arguments : $"-m pip {arguments}",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			WorkingDirectory = _pythonHome
		};

		using var process = Process.Start(startInfo) ?? throw new NullReferenceException($"Unable to run '{filename}'.");
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			throw new Exception($"Pip command failed: {error}");
		}

		return output;
	}

	#endregion
}