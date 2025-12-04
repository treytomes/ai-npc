using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AINPC;

public sealed class OllamaInstaller
{
	#region Fields

	private readonly HttpClient _httpClient;
	private readonly Action<string>? _log;

	#endregion

	#region Constructors

	public OllamaInstaller(HttpClient httpClient, Action<string>? logger = null)
	{
		_httpClient = httpClient;
		_log = logger;
	}

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
			Log($"Ollama already installed at: {ollamaExe}");
			return ollamaExe;
		}

		Log("Ollama not found. Installing…");

		Directory.CreateDirectory(installDir);

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return await InstallWindowsAsync(installDir);

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			return await InstallLinuxAsync(installDir);

		Log("Unsupported OS.");
		return null;
	}

	// ---------------------------
	// WINDOWS INSTALL
	// ---------------------------
	private async Task<string?> InstallWindowsAsync(string installDir)
	{
		// Official static build tarball
		const string URL = "https://ollama.ai/download/ollama-windows-amd64.zip";

		var zipPath = Path.Combine(installDir, "ollama.zip");

		try
		{
			Log($"Downloading Ollama Windows build from: {URL}");
			await DownloadFileAsync(URL, zipPath);

			Log("Extracting...");
			System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, installDir, overwriteFiles: true);

			File.Delete(zipPath);

			// Windows build ships with "ollama.exe"
			var exe = Path.Combine(installDir, "ollama.exe");

			if (!File.Exists(exe))
			{
				Log("Install failed: ollama.exe missing.");
				return null;
			}

			Log($"Ollama installed at: {exe}");
			return exe;
		}
		catch (Exception ex)
		{
			Log($"Windows install error: {ex}");
			return null;
		}
	}

	// ---------------------------
	// LINUX INSTALL
	// ---------------------------
	private async Task<string?> InstallLinuxAsync(string installDir)
	{
		// Official static Linux tgz
		const string URL = "https://ollama.ai/download/ollama-linux-amd64.tgz";

		var tgzPath = Path.Combine(installDir, "ollama.tgz");

		try
		{
			Log($"Downloading Ollama Linux build from: {URL}");
			await DownloadFileAsync(URL, tgzPath);

			Log("Extracting tarball…");

			// Extract using system 'tar'
			var result = RunProcess("tar", $"-xzf \"{tgzPath}\" -C \"{installDir}\"");
			if (!string.IsNullOrWhiteSpace(result))
				Log(result);

			File.Delete(tgzPath);

			// The extracted tar places "ollama" binary directly.
			var exe = Path.Combine(installDir, "bin", "ollama");

			if (!File.Exists(exe))
			{
				Log("Install failed: ollama binary missing.");
				return null;
			}

			// Ensure executable permissions
			RunProcess("chmod", $"+x \"{exe}\"");

			Log($"Ollama installed at: {exe}");
			return exe;
		}
		catch (Exception ex)
		{
			Log($"Linux install error: {ex}");
			return null;
		}
	}

	// ---------------------------
	// UTILITY HELPERS
	// ---------------------------
	private async Task DownloadFileAsync(string url, string filePath)
	{
		const int maxRetries = 4;
		const int bufferSize = 64 * 1024; // 64 KB
		int attempt = 0;

		while (true)
		{
			attempt++;

			try
			{
				Console.WriteLine($"[OllamaInstaller] Starting download attempt {attempt}/{maxRetries}");

				long existingSize = 0;
				if (File.Exists(filePath))
				{
					existingSize = new FileInfo(filePath).Length;
					Console.WriteLine($"[OllamaInstaller] Found partial file ({existingSize} bytes). Resuming…");
				}

				using var request = new HttpRequestMessage(HttpMethod.Get, url);

				if (existingSize > 0)
					request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingSize, null);

				using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();

				long? totalSize = response.Content.Headers.ContentLength;
				long totalBytesExpected = (totalSize.HasValue ? existingSize + totalSize.Value : -1);

				Console.WriteLine($"[OllamaInstaller] Total file size: " +
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

				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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

						Console.WriteLine(
							$"[OllamaInstaller] Downloading… {progressStr} of {totalStr} " +
							$"({speedMbPerSec:0.0} MB/s)"
						);
					}
				}

				Console.WriteLine("[OllamaInstaller] Download completed successfully.");
				return;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[OllamaInstaller] Download failed: {ex.Message}");

				if (attempt >= maxRetries)
				{
					Console.WriteLine("[OllamaInstaller] Max retries reached — deleting corrupted file.");
					if (File.Exists(filePath))
						File.Delete(filePath);

					throw;
				}

				int delay = 1000 * attempt;
				Console.WriteLine($"[OllamaInstaller] Retrying in {delay} ms…");
				await Task.Delay(delay);
			}
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

	private void Log(string message)
	{
		_log?.Invoke($"[OllamaInstaller] {message}");
	}

	private static string GetInstallDir()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			return Path.Combine(baseDir, "AINPC", "ollama");
		}

		// Linux/macOS-like path
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.Combine(home, ".ainpc", "ollama");
	}

	private static string GetOllamaExecutablePath(string installDir)
	{
		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? Path.Combine(installDir, "ollama.exe")
			: Path.Combine(installDir, "bin", "ollama");
	}

	#endregion
}
