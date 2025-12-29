using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Adventure.LLM.OllamaRuntime;

public sealed class OllamaProcess
{
	#region Fields

	private readonly ILogger<OllamaProcess> _logger;
	private Process? _process = null;

	#endregion

	#region Constructors

	public OllamaProcess(ILogger<OllamaProcess> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	#endregion

	#region Properties

	public int ProcessId => _process?.Id ?? -1;

	#endregion

	#region Methods

	/// <summary>
	/// Runs a single Ollama command (e.g., "pull llama3:8b").
	/// Collects all stdout/stderr into a single string buffer.
	/// Fires optional line callbacks.
	/// </summary>
	public async Task<string> RunAsync(string ollamaPath, string arguments, CancellationToken ct = default, int timeoutMs = 60000)
	{
		var psi = new ProcessStartInfo
		{
			FileName = ollamaPath,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		_process = new Process { StartInfo = psi, EnableRaisingEvents = false };

		var sb = new StringBuilder();

		try
		{
			_logger.LogInformation($"Executing: ollama {arguments}");
			_process.Start();

			var outputTask = Task.Run(async () =>
			{
				while (!_process.HasExited)
				{
					var line = await _process.StandardOutput.ReadLineAsync();
					if (line == null) break;

					sb.AppendLine(line);
					_logger.LogInformation($"[ollama] {line}");
				}
			}, ct);

			var errorTask = Task.Run(async () =>
			{
				while (!_process.HasExited)
				{
					var line = await _process.StandardError.ReadLineAsync();
					if (line == null) break;

					sb.AppendLine(line);
					_logger.LogInformation($"[ollama] {line}");
				}
			}, ct);

			var waitTask = Task.Run(() => _process.WaitForExit(timeoutMs), ct);

			await Task.WhenAll(outputTask, errorTask, waitTask);

			if (!_process.HasExited)
			{
				try { _process.Kill(); } catch { }
				sb.AppendLine("Process timed out.");
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine(ex.ToString());
			_logger.LogError($"Process error: {ex}");
		}
		finally
		{
			_process.Dispose();
			_process = null;
		}

		return sb.ToString().Trim();
	}

	#endregion
}