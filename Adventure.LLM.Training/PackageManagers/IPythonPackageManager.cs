namespace Adventure.LLM.Training.PackageManagers;

internal interface IPythonPackageManager
{
	Task<bool> IsPackageInstalled(string packageName);
	Task InstallPackageWithDependencies(string packageSpec);
	Task InstallFromRequirements(string requirementsPath);
}
