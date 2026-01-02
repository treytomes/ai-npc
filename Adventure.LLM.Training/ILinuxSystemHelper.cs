namespace Adventure.LLM.Training;

internal interface ILinuxSystemHelper : IDisposable
{
	IObservable<OutputReceivedEventArgs> WhenOutputReceived { get; }

	bool IsInteractiveTerminal();
	Task<bool> EnsurePythonDependencies();
	Task<bool> IsPackageInstalled(string packageName);
	Task<bool> InstallSystemPackages(IEnumerable<string> packages);
}
