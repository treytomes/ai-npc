using System.Diagnostics;
using System.Reactive.Subjects;

namespace Adventure.LLM.Training.Installers;

internal abstract class PythonInstaller : IPythonInstaller
{
	#region Fields

	private readonly Subject<ProgressChangedEventArgs> _progressChangedSubject = new();
	private readonly Subject<OutputReceivedEventArgs> _outputReceivedSubject = new();

	protected readonly string _appDataPath;
	protected readonly string _pythonVersion = "3.11.7";
	private bool _disposedValue = false;
	protected readonly ITextReader _passwordReader;
	private readonly HttpClient _httpClient = new();

	#endregion

	#region Constructors

	protected PythonInstaller(ITextReader passwordReader)
	{
		_passwordReader = passwordReader ?? throw new ArgumentNullException(nameof(passwordReader));
		_appDataPath = GetInstallDir();
	}

	#endregion

	#region Properties

	public IObservable<ProgressChangedEventArgs> WhenProgressChanged => _progressChangedSubject;
	public IObservable<OutputReceivedEventArgs> WhenOutputReceived => _outputReceivedSubject;

	#endregion

	#region Methods

	protected abstract string GetInstallDir();

	/// <summary>
	/// Ensure system dependencies are in place before proceeding with the Python install.
	/// </summary>
	protected virtual async Task EnsureDependencies()
	{
	}

	public async Task<string> InstallPythonAsync()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");

		if (Directory.Exists(pythonPath))
		{
			ReportProgress(100, "Python already installed.");
			return pythonPath;
		}

		await EnsureDependencies();

		Directory.CreateDirectory(_appDataPath);

		// Download with progress.
		var archiveUrl = GetPythonDownloadUrl();
		var archivePath = Path.Combine(_appDataPath, Path.GetFileName(archiveUrl));

		await DownloadWithProgress(archiveUrl, archivePath);

		// Extract.
		ReportProgress(80, "Extracting Python...");
		await ExtractPython(archivePath, pythonPath);
		File.Delete(archivePath);

		// Configure.
		ReportProgress(90, "Configuring Python environment...");
		await ConfigurePythonEnvironment(pythonPath);

		ReportProgress(100, "Python installation complete!");
		return pythonPath;
	}

	protected abstract string GetPythonDownloadUrl();

	protected abstract Task ExtractPython(string archivePath, string destinationPath);

	protected abstract Task ConfigurePythonEnvironment(string pythonPath);

	protected virtual ProcessStartInfo PreprocessStartInfo(ProcessStartInfo startInfo)
	{
		return startInfo;
	}

	protected async Task RunCommandWithOutput(string fileName, string arguments, bool showRealTimeOutput = false)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = arguments,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		startInfo = PreprocessStartInfo(startInfo);

		using var process = Process.Start(startInfo) ?? throw new NullReferenceException($"Unable to run '{fileName}");

		// Create tasks to read stdout and stderr simultaneously
		var outputTask = Task.Run(async () =>
		{
			while (!process.StandardOutput.EndOfStream)
			{
				var line = await process.StandardOutput.ReadLineAsync();
				if (line != null && showRealTimeOutput)
				{
					ReportOutput(line);
				}
			}
		});

		var errorTask = Task.Run(async () =>
		{
			while (!process.StandardError.EndOfStream)
			{
				var line = await process.StandardError.ReadLineAsync();
				if (line != null && showRealTimeOutput)
				{
					ReportOutput($"[ERROR] {line}");
				}
			}
		});

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			string error = await process.StandardError.ReadToEndAsync();
			throw new Exception($"Command failed: {fileName} {arguments}\nError: {error}");
		}
	}

	protected void ReportProgress(int percentage, string message)
	{
		_progressChangedSubject.OnNext(new(percentage, message));
	}

	protected void ReportOutput(string message)
	{
		_outputReceivedSubject.OnNext(new(message));
	}

	public abstract string GetPythonExecutablePath();

	public abstract string GetPipExecutablePath();

	private async Task DownloadWithProgress(string url, string destinationPath)
	{
		using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		var totalBytes = response.Content.Headers.ContentLength ?? -1L;
		var canReportProgress = totalBytes != -1;

		using var contentStream = await response.Content.ReadAsStreamAsync();
		using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

		var buffer = new byte[8192];
		var totalRead = 0L;
		var read = 0;

		while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
		{
			await fileStream.WriteAsync(buffer, 0, read);
			totalRead += read;

			if (canReportProgress)
			{
				var progress = (int)((totalRead * 70L) / totalBytes);
				ReportProgress(progress, $"Downloading {url}... {totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB");
			}
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_progressChangedSubject.OnCompleted();
				_progressChangedSubject.Dispose();

				_outputReceivedSubject.OnCompleted();
				_outputReceivedSubject.Dispose();
			}

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}