using System.Diagnostics;

namespace Adventure.LLM.Training;

internal sealed class LinuxPythonHelper
{
	public static async Task<bool> EnsureDependencies()
	{
		var requiredPackages = new[]
		{
			"build-essential",
			"libssl-dev",
			"zlib1g-dev",
			"libncurses5-dev",
			"libncursesw5-dev",
			"libreadline-dev",
			"libsqlite3-dev",
			"libgdbm-dev",
			"libdb5.3-dev",
			"libbz2-dev",
			"libexpat1-dev",
			"liblzma-dev",
			"libffi-dev",
			"uuid-dev",
			"python3-dev",
			"python3-pip",
		};

		Console.WriteLine("Checking for required system packages...");

		var missingPackages = new List<string>();

		foreach (var package in requiredPackages)
		{
			if (!await IsPackageInstalled(package))
			{
				missingPackages.Add(package);
			}
		}

		if (missingPackages.Count == 0)
		{
			Console.WriteLine("All required packages are already installed.");
			return true;
		}

		Console.WriteLine($"Missing {missingPackages.Count} required package(s):");
		foreach (var pkg in missingPackages)
		{
			Console.WriteLine($"  - {pkg}");
		}
		Console.WriteLine();

		// Check if we have sudo access
		bool hasSudoAccess = await CheckSudoAccess();

		// If no cached sudo access, request it
		if (!hasSudoAccess)
		{
			Console.WriteLine("Sudo access is required to install system packages.");
			Console.WriteLine("Requesting sudo privileges...");

			hasSudoAccess = await RequestSudoAccess();

			if (!hasSudoAccess)
			{
				Console.WriteLine();
				Console.WriteLine("Error: Cannot install system packages without sudo access.");
				Console.WriteLine("Please run the following command manually:");
				Console.WriteLine();
				Console.WriteLine($"  sudo apt-get update && sudo apt-get install -y {string.Join(" ", missingPackages)}");
				Console.WriteLine();
				return false;
			}
		}

		Console.WriteLine("Installing missing packages...");
		bool success = await InstallSystemPackages(missingPackages);

		if (!success)
		{
			Console.WriteLine("Warning: Some packages failed to install.");
			Console.WriteLine("You may need to install them manually:");
			Console.WriteLine($"  sudo apt-get install -y {string.Join(" ", missingPackages)}");
			return false;
		}

		Console.WriteLine("All dependencies installed successfully.");
		return true;
	}

	private static async Task<bool> CheckSudoAccess()
	{
		try
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = "sudo",
				Arguments = "-n true",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			}) ?? throw new NullReferenceException("Unable to run 'sudo'.");

			await process.WaitForExitAsync();
			return process.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	private static async Task<bool> RequestSudoAccess()
	{
		try
		{
			// First, try with a simple sudo command that requires password
			// This will cache sudo credentials for subsequent commands
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "sudo",
					Arguments = "-v", // Validates and refreshes sudo credentials
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = false,
					CreateNoWindow = false // Show terminal for password prompt
				}
			};

			process.Start();
			await process.WaitForExitAsync();

			if (process.ExitCode == 0)
			{
				Console.WriteLine("Sudo access granted.");
				return true;
			}

			// If -v didn't work, try an interactive approach
			Console.WriteLine("Attempting interactive sudo request...");

			var interactiveProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "sudo",
					Arguments = "echo 'Sudo access granted'",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = false,
					RedirectStandardInput = false,
					CreateNoWindow = false
				}
			};

			interactiveProcess.Start();
			string output = await interactiveProcess.StandardOutput.ReadToEndAsync();
			await interactiveProcess.WaitForExitAsync();

			if (interactiveProcess.ExitCode == 0)
			{
				Console.WriteLine("Sudo access granted.");
				return true;
			}

			return false;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to request sudo access: {ex.Message}");
			return false;
		}
	}

	private static async Task<bool> IsPackageInstalled(string packageName)
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

	private static async Task<bool> InstallSystemPackages(List<string> packages)
	{
		try
		{
			Console.WriteLine("Updating package lists...");

			// Update package list first
			var updateProcess = Process.Start(new ProcessStartInfo
			{
				FileName = "sudo",
				Arguments = "apt-get update",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = false // Show output
			}) ?? throw new NullReferenceException("Unable to run 'sudo'.");

			// Stream output
			var updateOutputTask = Task.Run(async () =>
			{
				while (!updateProcess.StandardOutput.EndOfStream)
				{
					var line = await updateProcess.StandardOutput.ReadLineAsync();
					if (!string.IsNullOrWhiteSpace(line))
					{
						Console.WriteLine($"  {line}");
					}
				}
			});

			await updateProcess.WaitForExitAsync();
			await updateOutputTask;

			if (updateProcess.ExitCode != 0)
			{
				Console.WriteLine("Warning: apt-get update failed, continuing anyway...");
			}

			Console.WriteLine();
			Console.WriteLine("Installing packages (this may take a few minutes)...");

			// Install all packages in one command for efficiency
			var installProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "sudo",
					Arguments = $"apt-get install -y {string.Join(" ", packages)}",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = false
				}
			};

			installProcess.Start();

			// Stream output to show progress
			var installOutputTask = Task.Run(async () =>
			{
				while (!installProcess.StandardOutput.EndOfStream)
				{
					var line = await installProcess.StandardOutput.ReadLineAsync();
					if (!string.IsNullOrWhiteSpace(line))
					{
						Console.WriteLine($"  {line}");
					}
				}
			});

			await installProcess.WaitForExitAsync();
			await installOutputTask;

			if (installProcess.ExitCode != 0)
			{
				string error = await installProcess.StandardError.ReadToEndAsync();
				Console.WriteLine($"Installation error: {error}");
				return false;
			}

			Console.WriteLine();
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Exception during package installation: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Checks if running in a terminal that supports interactive input
	/// </summary>
	public static bool IsInteractiveTerminal()
	{
		try
		{
			// Check if stdin is redirected
			return !Console.IsInputRedirected && !Console.IsOutputRedirected;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Alternative method that uses pkexec (GUI sudo) if available
	/// Useful for GUI applications
	/// </summary>
	public static async Task<bool> RequestSudoAccessGUI()
	{
		try
		{
			// Check if pkexec is available (common on Ubuntu/Debian with GUI)
			var whichProcess = Process.Start(new ProcessStartInfo
			{
				FileName = "which",
				Arguments = "pkexec",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			}) ?? throw new NullReferenceException("Unable to run 'which'.");

			await whichProcess.WaitForExitAsync();

			if (whichProcess.ExitCode == 0)
			{
				Console.WriteLine("Using graphical authentication (pkexec)...");

				var pkexecProcess = Process.Start(new ProcessStartInfo
				{
					FileName = "pkexec",
					Arguments = "echo 'Access granted'",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = false
				}) ?? throw new NullReferenceException("Unable to run 'pkexec'.");

				await pkexecProcess.WaitForExitAsync();

				if (pkexecProcess.ExitCode == 0)
				{
					// Cache the credentials with sudo
					await RequestSudoAccess();
					return true;
				}
			}

			return false;
		}
		catch
		{
			return false;
		}
	}
}