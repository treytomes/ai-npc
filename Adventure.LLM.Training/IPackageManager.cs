namespace Adventure.LLM.Training;

internal interface IPackageManager : IDisposable
{
	IObservable<OutputReceivedEventArgs> WhenOutputReceived { get; }

	Task<bool> EnsurePackagesAsync(IEnumerable<string> requiredPackages);
	Task<bool> IsPackageInstalledAsync(string packageName);
	Task<bool> InstallPackagesAsync(IEnumerable<string> packages);
}
