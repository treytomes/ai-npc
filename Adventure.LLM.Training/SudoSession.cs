using System.Diagnostics;
using System.Reactive.Subjects;

namespace Adventure.LLM.Training;

internal class SudoSession : IDisposable
{
	#region Fields

	private readonly ITextReader _passwordReader;
	private readonly Subject<OutputReceivedEventArgs> _outputReceived = new();
	private bool _disposed;
	private bool _hasAccess;
	private bool _usePkexec;

	#endregion

	#region Constructors

	public SudoSession(ITextReader passwordReader)
	{
		_passwordReader = passwordReader ?? throw new ArgumentNullException(nameof(passwordReader));
	}

	#endregion

	#region Properties

	public IObservable<OutputReceivedEventArgs> WhenOutputReceived => _outputReceived;

	/// <summary>
	/// Gets whether to use pkexec instead of sudo for privileged operations
	/// </summary>
	public bool UsePkexec => _usePkexec;

	#endregion

	#region Methods

	public async Task<bool> ActivateAsync()
	{
		// Check if we already have sudo access
		if (await CheckSudoAccess())
		{
			Report("Sudo access already available.");
			_hasAccess = true;
			_usePkexec = false;
			return true;
		}

		// Check if pkexec is available
		if (await IsPkexecAvailable())
		{
			Report("PolicyKit (pkexec) is available for authentication.");
			_hasAccess = true;
			_usePkexec = true;
			return true;
		}

		// Try graphical sudo methods
		Report("Requesting sudo access...");
		if (await RequestGraphicalSudoAccess())
		{
			_hasAccess = true;
			_usePkexec = false;
			return true;
		}

		// Fall back to console-based sudo
		Report("Graphical sudo failed, trying console-based sudo...");
		if (await RequestConsoleSudoAccess())
		{
			_hasAccess = true;
			_usePkexec = false;
			return true;
		}

		Report("Failed to obtain sudo access.");
		return false;
	}

	private async Task<bool> IsPkexecAvailable()
	{
		try
		{
			var checkProcess = Process.Start(new ProcessStartInfo
			{
				FileName = "which",
				Arguments = "pkexec",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			});

			if (checkProcess == null)
				return false;

			await checkProcess.WaitForExitAsync();
			return checkProcess.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	private async Task<bool> CheckSudoAccess()
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

	private async Task<bool> RequestGraphicalSudoAccess()
	{
		try
		{
			// Try with -A flag to use graphical sudo helper if available
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "sudo",
					Arguments = "-A -v", // -A uses SUDO_ASKPASS if set, -v validates credentials
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};

			// Set SUDO_ASKPASS environment variable if not already set
			if (!process.StartInfo.Environment.ContainsKey("SUDO_ASKPASS"))
			{
				// Try common graphical sudo helpers
				var askpassHelpers = new[]
				{
					"/usr/lib/ssh/x11-ssh-askpass",
					"/usr/bin/ssh-askpass",
					"/usr/lib/openssh/gnome-ssh-askpass",
					"/usr/bin/ksshaskpass",
					"/usr/bin/lxqt-openssh-askpass"
				};

				foreach (var helper in askpassHelpers)
				{
					if (File.Exists(helper))
					{
						process.StartInfo.Environment["SUDO_ASKPASS"] = helper;
						break;
					}
				}
			}

			process.Start();
			await process.WaitForExitAsync();

			if (process.ExitCode == 0)
			{
				Report("Sudo access granted (graphical).");
				return true;
			}
		}
		catch (Exception ex)
		{
			Report($"Graphical sudo failed: {ex.Message}");
		}

		return false;
	}

	private async Task<bool> RequestConsoleSudoAccess()
	{
		try
		{
			Report("Please enter your password for sudo access: ");

			var password = _passwordReader.Read();
			Report(Environment.NewLine); // New line after password input

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "sudo",
					Arguments = "-S -v", // -S reads password from stdin, -v validates
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					CreateNoWindow = true
				}
			};

			process.Start();

			// Write password to stdin
			await process.StandardInput.WriteLineAsync(password);
			process.StandardInput.Close();

			await process.WaitForExitAsync();

			// Clear password from memory
			password = null;

			if (process.ExitCode == 0)
			{
				Report("Sudo access granted (console).");
				return true;
			}
			else
			{
				var error = await process.StandardError.ReadToEndAsync();
				Report($"Sudo authentication failed: {error}");
			}
		}
		catch (Exception ex)
		{
			Report($"Console sudo failed: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Executes a command with elevated privileges using the appropriate method
	/// </summary>
	public async Task<ProcessResult> ExecuteElevatedAsync(string command, string arguments)
	{
		if (!_hasAccess)
			throw new InvalidOperationException("Sudo session not activated");

		var startInfo = new ProcessStartInfo
		{
			FileName = _usePkexec ? "pkexec" : "sudo",
			Arguments = _usePkexec ? $"{command} {arguments}" : $"{command} {arguments}",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		var process = Process.Start(startInfo);
		if (process == null)
			throw new InvalidOperationException($"Failed to start {startInfo.FileName}");

		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		return new ProcessResult
		{
			ExitCode = process.ExitCode,
			StandardOutput = output,
			StandardError = error
		};
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				if (_hasAccess && !_usePkexec)
				{
					try
					{
						// Invalidate sudo credentials (only for sudo, not pkexec)
						var process = Process.Start(new ProcessStartInfo
						{
							FileName = "sudo",
							Arguments = "-k", // Kill sudo timestamp
							UseShellExecute = false,
							CreateNoWindow = true
						});

						process?.WaitForExit(5000); // Wait max 5 seconds
						Report("Sudo access revoked.");
					}
					catch (Exception ex)
					{
						Report($"Failed to revoke sudo access: {ex.Message}");
					}
				}

				_outputReceived.OnCompleted();
				_outputReceived.Dispose();
			}

			_disposed = true;
		}
	}

	private void Report(string message)
	{
		_outputReceived.OnNext(new(message));
	}

	#endregion
}

public class ProcessResult
{
	public int ExitCode { get; init; }
	public string StandardOutput { get; init; } = string.Empty;
	public string StandardError { get; init; } = string.Empty;
	public bool Success => ExitCode == 0;
}