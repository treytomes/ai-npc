namespace Adventure.LLM.Training.Installers;

internal interface IPythonInstaller : IDisposable
{
	IObservable<ProgressChangedEventArgs> WhenProgressChanged { get; }
	IObservable<OutputReceivedEventArgs> WhenOutputReceived { get; }

	Task<string> InstallPythonAsync();
	string GetPipExecutablePath();
	string GetPythonExecutablePath();
}
