using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Adventure.LLM.Ollama;

internal sealed class WindowsOllamaInstaller(HttpClient httpClient, ILogger<WindowsOllamaInstaller> logger)
	: OllamaInstaller(httpClient, logger)
{
	/// <summary>
	/// Official static build.
	/// </summary>
	private const string URL = "https://ollama.ai/download/ollama-windows-amd64.zip";

	protected override async Task<string?> InstallAsync(string installDir)
	{
		var zipPath = Path.Combine(installDir, "ollama.zip");

		try
		{
			_logger.LogInformation($"Downloading Ollama Windows build from: {URL}");
			await DownloadFileAsync(URL, zipPath);

			_logger.LogInformation("Extracting...");
			System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, installDir, overwriteFiles: true);

			File.Delete(zipPath);

			// Windows build ships with "ollama.exe"
			var exe = Path.Combine(installDir, "ollama.exe");

			if (!File.Exists(exe))
			{
				_logger.LogError("Install failed: ollama.exe missing.");
				return null;
			}

			_logger.LogInformation($"Ollama installed at: {exe}");
			return exe;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Windows install error: {ex}");
			return null;
		}
	}

	protected override string GetInstallDir() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Adventure",
			"ollama"
		);

	protected override string GetOllamaExecutablePath(string installDir) =>
		Path.Combine(installDir, "ollama.exe");
}
