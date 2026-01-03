namespace Adventure.LLM.Training.PackageManagers;

internal sealed class LinuxPythonPackageManager : PythonPackageManager
{
	public LinuxPythonPackageManager(string pythonHome)
		: base(pythonHome, Path.Combine(pythonHome, "bin", "python3"), Path.Combine(pythonHome, "bin", "pip3"))
	{
	}

	protected override string GetInstallArgs() => "--prefer-binary";
}
