using System.Diagnostics;

namespace Adventure.Services;

class ProcessService : IProcessService
{
	private int WAIT_TIME_MS = 2500;

	/// <inheritdoc/>
	public string RunProcess(string exe, string args)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = exe,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};

			using var p = Process.Start(psi);
			if (p == null)
				return "";

			var stdout = p.StandardOutput.ReadToEnd();
			var stderr = p.StandardError.ReadToEnd();

			p.WaitForExit(WAIT_TIME_MS);

			return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
		}
		catch
		{
			return "";
		}
	}
}