namespace Adventure.LLM.Training.EnvironmentManagers;

internal interface IPythonEnvironmentManager : IDisposable
{
	public IObservable<OutputReceivedEventArgs> WhenOutputReceived { get; }

	Task<bool> SetupEnvironmentAsync();
	void Initialize();
	void Shutdown();
	string? GetPythonHome();
	bool VerifyPythonLibrary();
}
