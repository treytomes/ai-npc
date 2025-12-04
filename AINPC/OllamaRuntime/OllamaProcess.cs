using System.Diagnostics;
using System.Text;

namespace AINPC;

public sealed class OllamaProcess
{
	private readonly string _ollamaPath;
	private readonly Action<string>? _log;

	public OllamaProcess(string ollamaPath, Action<string>? logger = null)
	{
		_ollamaPath = ollamaPath;
		_log = logger;
	}

	/// <summary>
	/// Runs a single Ollama command (e.g., "pull llama3:8b").
	/// Collects all stdout/stderr into a single string buffer.
	/// Fires optional line callbacks.
	/// </summary>
	public async Task<string> RunAsync(
		string arguments,
		Action<string>? onLine = null,
		CancellationToken ct = default,
		int timeoutMs = 60000)
	{
		var psi = new ProcessStartInfo
		{
			FileName = _ollamaPath,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};

		var process = new Process { StartInfo = psi, EnableRaisingEvents = false };

		var sb = new StringBuilder();

		try
		{
			Log($"Executing: ollama {arguments}");
			process.Start();

			var outputTask = Task.Run(async () =>
			{
				while (!process.HasExited)
				{
					var line = await process.StandardOutput.ReadLineAsync();
					if (line == null) break;

					sb.AppendLine(line);
					Log($"[ollama] {line}");
					onLine?.Invoke(line);
				}
			}, ct);

			var errorTask = Task.Run(async () =>
			{
				while (!process.HasExited)
				{
					var line = await process.StandardError.ReadLineAsync();
					if (line == null) break;

					sb.AppendLine(line);
					Log($"[ollama] {line}");
					onLine?.Invoke(line);
				}
			}, ct);

			var waitTask = Task.Run(() => process.WaitForExit(timeoutMs), ct);

			await Task.WhenAll(outputTask, errorTask, waitTask);

			if (!process.HasExited)
			{
				try { process.Kill(); } catch { }
				sb.AppendLine("Process timed out.");
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine(ex.ToString());
			Log($"Process error: {ex}");
		}
		finally
		{
			process.Dispose();
		}

		return sb.ToString().Trim();
	}

	private void Log(string msg)
	{
		_log?.Invoke($"[OllamaProcess] {msg}");
	}
}