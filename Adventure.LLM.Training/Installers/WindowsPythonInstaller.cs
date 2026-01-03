using System.IO.Compression;

namespace Adventure.LLM.Training.Installers;

internal sealed class WindowsPythonInstaller(string appName, ITextReader passwordReader)
	: PythonInstaller(passwordReader)
{
	private readonly string _appName = appName;

	protected override string GetPythonDownloadUrl() =>
		$"https://www.python.org/ftp/python/{_pythonVersion}/python-{_pythonVersion}-embed-amd64.zip";

	protected override string GetInstallDir() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			_appName
		);

	protected override async Task ExtractPython(string archivePath, string destinationPath) =>
		await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, destinationPath));

	protected override async Task ConfigurePythonEnvironment(string pythonPath)
	{
		var pthFile = Directory.GetFiles(pythonPath, "*.pth").FirstOrDefault();
		if (pthFile != null)
		{
			string content = File.ReadAllText(pthFile);
			content = content.Replace("#import site", "import site");
			content += $"\nLib\\site-packages\n";
			File.WriteAllText(pthFile, content);
		}

		string sitePackages = Path.Combine(pythonPath, "Lib", "site-packages");
		Directory.CreateDirectory(sitePackages);

		string pythonExe = Path.Combine(pythonPath, "python.exe");
		await RunCommandWithOutput(pythonExe, "-m ensurepip --upgrade");
		await RunCommandWithOutput(pythonExe, "-m pip install --upgrade pip");
	}

	public override string GetPythonExecutablePath()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");
		return Path.Combine(pythonPath, "python.exe");
	}

	public override string GetPipExecutablePath()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");
		return Path.Combine(pythonPath, "Scripts", "pip.exe");
	}
}
