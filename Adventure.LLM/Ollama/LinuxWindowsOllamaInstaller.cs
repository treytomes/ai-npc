using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Adventure.LLM.Ollama;

internal sealed class LinuxOllamaInstaller(HttpClient httpClient, ILogger<OllamaInstaller> logger)
	: OllamaInstaller(httpClient, logger)
{
	/// <summary>
	/// Official static Linux tgz.
	/// </summary>
	private const string URL = "https://ollama.ai/download/ollama-linux-amd64.tgz";

	protected override async Task<string?> InstallAsync(string installDir)
	{
		var tgzPath = Path.Combine(installDir, "ollama.tgz");

		try
		{
			_logger.LogInformation($"Downloading Ollama Linux build from: {URL}");
			await DownloadFileAsync(URL, tgzPath);

			_logger.LogInformation("Extracting tarball…");

			// Extract using system 'tar'
			var result = RunProcess("tar", $"-xzf \"{tgzPath}\" -C \"{installDir}\"");
			if (!string.IsNullOrWhiteSpace(result))
				_logger.LogInformation(result);

			File.Delete(tgzPath);

			// The extracted tar places "ollama" binary directly.
			var exe = Path.Combine(installDir, "bin", "ollama");

			if (!File.Exists(exe))
			{
				_logger.LogError("Install failed: ollama binary missing.");
				return null;
			}

			// Ensure executable permissions
			RunProcess("chmod", $"+x \"{exe}\"");

			_logger.LogInformation($"Ollama installed at: {exe}");
			return exe;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Linux install error: {ex}");
			return null;
		}
	}

	private static string RunProcess(string exe, string args)
	{
		try
		{
			var psi = new ProcessStartInfo()
			{
				FileName = exe,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};

			using var p = Process.Start(psi);
			if (p == null) return "";

			var stdout = p.StandardOutput.ReadToEnd();
			var stderr = p.StandardError.ReadToEnd();

			p.WaitForExit(4000);

			return $"{stdout}{stderr}".Trim();
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}
	}

	protected override string GetInstallDir() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".adventure",
			"ollama"
		);

	protected override string GetOllamaExecutablePath(string installDir) =>
		Path.Combine(installDir, "bin", "ollama");
}

internal abstract class OllamaInstaller(HttpClient httpClient, ILogger<OllamaInstaller> logger)
{
	#region Fields

	private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	protected readonly ILogger<OllamaInstaller> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	#endregion

	#region Methods

	/// <summary>
	/// Entry point: Ensure Ollama is installed & return the path to the ollama executable.
	/// </summary>
	public async Task<string?> EnsureInstalledAsync()
	{
		var installDir = GetInstallDir();
		var ollamaExe = GetOllamaExecutablePath(installDir);

		if (File.Exists(ollamaExe))
		{
			_logger.LogInformation($"Ollama already installed at: {ollamaExe}");
			return ollamaExe;
		}

		_logger.LogInformation("Ollama not found. Installing…");

		Directory.CreateDirectory(installDir);

		return await InstallAsync(installDir);
	}

	protected abstract Task<string?> InstallAsync(string installDir);

	protected async Task DownloadFileAsync(string url, string filePath)
	{
		const int maxRetries = 4;
		const int bufferSize = 64 * 1024; // 64 KB
		int attempt = 0;

		while (true)
		{
			attempt++;

			try
			{
				_logger.LogInformation($"[OllamaInstaller] Starting download attempt {attempt}/{maxRetries}");

				long existingSize = 0;
				if (File.Exists(filePath))
				{
					existingSize = new FileInfo(filePath).Length;
					_logger.LogInformation($"[OllamaInstaller] Found partial file ({existingSize} bytes). Resuming…");
				}

				using var request = new HttpRequestMessage(HttpMethod.Get, url);

				if (existingSize > 0)
					request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingSize, null);

				using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();

				long? totalSize = response.Content.Headers.ContentLength;
				long totalBytesExpected = (totalSize.HasValue ? existingSize + totalSize.Value : -1);

				_logger.LogInformation($"[OllamaInstaller] Total file size: " +
								(totalBytesExpected > 0 ? $"{totalBytesExpected:N0} bytes" : "unknown"));

				using var networkStream = await response.Content.ReadAsStreamAsync();
				using var fileStream = new FileStream(
					filePath,
					FileMode.Append,
					FileAccess.Write,
					FileShare.None,
					bufferSize,
					useAsync: true
				);

				byte[] buffer = new byte[bufferSize];
				long bytesDownloaded = existingSize;
				int bytesRead;

				var stopwatch = Stopwatch.StartNew();
				long lastProgressReportTime = 0;

				while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await fileStream.WriteAsync(buffer, 0, bytesRead);
					bytesDownloaded += bytesRead;

					long now = stopwatch.ElapsedMilliseconds;

					// Report progress every ~1s
					if (now - lastProgressReportTime >= 1000)
					{
						lastProgressReportTime = now;

						double progressPercent =
							(totalBytesExpected > 0)
							? (bytesDownloaded / (double)totalBytesExpected) * 100.0
							: -1;

						double mbDownloaded = bytesDownloaded / 1024.0 / 1024.0;
						double mbTotal = totalBytesExpected > 0 ? totalBytesExpected / 1024.0 / 1024.0 : -1;

						double speedMbPerSec = (bytesDownloaded - existingSize) / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds;

						string progressStr = progressPercent >= 0
							? $"{progressPercent:0.0}%"
							: $"{mbDownloaded:0.0} MB";

						string totalStr = mbTotal >= 0
							? $"{mbTotal:0.0} MB"
							: "? MB";

						_logger.LogInformation(
							$"[OllamaInstaller] Downloading… {progressStr} of {totalStr} " +
							$"({speedMbPerSec:0.0} MB/s)"
						);
					}
				}

				_logger.LogInformation("[OllamaInstaller] Download completed successfully.");
				return;
			}
			catch (Exception ex)
			{
				_logger.LogError($"[OllamaInstaller] Download failed: {ex.Message}");

				if (attempt >= maxRetries)
				{
					_logger.LogError("[OllamaInstaller] Max retries reached — deleting corrupted file.");
					if (File.Exists(filePath))
						File.Delete(filePath);

					throw;
				}

				int delay = 1000 * attempt;
				_logger.LogInformation($"[OllamaInstaller] Retrying in {delay} ms…");
				await Task.Delay(delay);
			}
		}
	}

	protected abstract string GetInstallDir();

	protected abstract string GetOllamaExecutablePath(string installDir);

	#endregion
}