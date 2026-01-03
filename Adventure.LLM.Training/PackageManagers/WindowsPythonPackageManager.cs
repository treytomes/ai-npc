namespace Adventure.LLM.Training.PackageManagers;

internal sealed class WindowsPythonPackageManager : PythonPackageManager
{
	public WindowsPythonPackageManager(string pythonHome)
		: base(pythonHome, Path.Combine(pythonHome, "python.exe"), Path.Combine(pythonHome, "Scripts", "pip.exe"))
	{
	}
}
