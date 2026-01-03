namespace Adventure.LLM.Training.PackageManagers;

internal interface IPythonPackageManager
{
	Task<bool> IsPackageInstalledAsync(string packageName);
	Task InstallPackageAsync(string packageSpec);
	Task InstallFromRequirementsAsync(string requirementsPath);
}
