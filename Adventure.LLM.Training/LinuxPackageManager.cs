using System.Diagnostics;
using System.Reactive.Subjects;

namespace Adventure.LLM.Training;

internal sealed class LinuxPackageManager : IPackageManager
{
	#region Fields

	private readonly Subject<OutputReceivedEventArgs> _outputReceivedSubject = new();
	private readonly ITextReader _passwordReader;
	private bool _disposedValue = false;

	#endregion

	#region Constructors

	public LinuxPackageManager(ITextReader passwordReader)
	{
		_passwordReader = passwordReader ?? throw new ArgumentNullException(nameof(passwordReader));
	}

	#endregion

	#region Properties

	public IObservable<OutputReceivedEventArgs> WhenOutputReceived => _outputReceivedSubject;

	#endregion

	#region Methods

	public async Task<bool> EnsurePackagesAsync(IEnumerable<string> requiredPackages)
	{
		ReportOutput("Checking for required system packages...");

		var missingPackages = new List<string>();

		foreach (var package in requiredPackages)
		{
			if (!await IsPackageInstalledAsync(package))
			{
				missingPackages.Add(package);
			}
		}

		if (missingPackages.Count == 0)
		{
			ReportOutput("All required packages are already installed.");
			return true;
		}

		ReportOutput($"Missing {missingPackages.Count} required package(s):");
		foreach (var pkg in missingPackages)
		{
			ReportOutput($"  - {pkg}");
		}
		ReportOutput(Environment.NewLine);

		// Use SudoSession to handle elevated access
		using var sudoSession = new SudoSession(_passwordReader);

		// Subscribe to sudo session output
		using var sudoOutputSubscription = sudoSession.WhenOutputReceived
			.Subscribe(e => ReportOutput(e.OutputText));

		if (!await sudoSession.ActivateAsync())
		{
			ReportOutput(Environment.NewLine);
			ReportOutput("Error: Cannot install system packages without elevated access.");
			ReportOutput("Please run the following command manually:");
			ReportOutput(Environment.NewLine);
			ReportOutput($"  sudo apt-get update && sudo apt-get install -y {string.Join(" ", missingPackages)}");
			ReportOutput(Environment.NewLine);
			return false;
		}

		ReportOutput("Installing missing packages...");
		bool success = await InstallPackagesAsync(missingPackages, sudoSession);

		if (!success)
		{
			ReportOutput("Warning: Some packages failed to install.");
			ReportOutput("You may need to install them manually:");
			ReportOutput($"  sudo apt-get install -y {string.Join(" ", missingPackages)}");
			return false;
		}

		ReportOutput("All dependencies installed successfully.");
		return true;
	}

	public async Task<bool> IsPackageInstalledAsync(string packageName)
	{
		try
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = "dpkg",
				Arguments = $"-s {packageName}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			}) ?? throw new NullReferenceException("Unable to run 'dpkg'.");

			await process.WaitForExitAsync();
			return process.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	public async Task<bool> InstallPackagesAsync(IEnumerable<string> packages)
	{
		using var sudoSession = new SudoSession(_passwordReader);
		return await InstallPackagesAsync(packages, sudoSession);
	}

	private void ReportOutput(string message)
	{
		_outputReceivedSubject.OnNext(new(message));
	}

	private async Task<bool> InstallPackagesAsync(IEnumerable<string> packages, SudoSession sudoSession)
	{
		try
		{
			ReportOutput("Updating package lists...");

			// Update package list first.
			var updateResult = await sudoSession.ExecuteElevatedAsync("apt-get", "update");

			if (!string.IsNullOrWhiteSpace(updateResult.StandardOutput))
			{
				foreach (var line in updateResult.StandardOutput.Split('\n'))
				{
					if (!string.IsNullOrWhiteSpace(line))
						ReportOutput($"  {line}");
				}
			}

			if (!updateResult.Success)
			{
				ReportOutput("Warning: apt-get update failed, continuing anyway...");
			}

			ReportOutput(Environment.NewLine);
			ReportOutput("Installing packages (this may take a few minutes)...");

			// Install all packages in one command for efficiency
			var installResult = await sudoSession.ExecuteElevatedAsync("apt-get", $"install -y {string.Join(" ", packages)}");

			if (!string.IsNullOrWhiteSpace(installResult.StandardOutput))
			{
				foreach (var line in installResult.StandardOutput.Split('\n'))
				{
					if (!string.IsNullOrWhiteSpace(line))
						ReportOutput($"  {line}");
				}
			}

			if (!installResult.Success)
			{
				ReportOutput($"Installation error: {installResult.StandardError}");
				return false;
			}

			ReportOutput(Environment.NewLine);
			return true;
		}
		catch (Exception ex)
		{
			ReportOutput($"Exception during package installation: {ex.Message}");
			return false;
		}
	}

	private void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_outputReceivedSubject.OnCompleted();
				_outputReceivedSubject.Dispose();
			}

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}