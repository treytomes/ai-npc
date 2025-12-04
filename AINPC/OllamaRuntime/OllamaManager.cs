using System.Diagnostics;
using AINPC.Gpu.Services;
using Microsoft.Extensions.Logging;

namespace AINPC;

sealed class OllamaManager : IDisposable
{
    #region Fields

    private readonly IGpuDetectorService _gpuDetector;
    private readonly OllamaInstaller _installer;
    private readonly ILogger<OllamaManager> _logger;

    private string? _ollamaPath;
    private Process? _serverProcess;
    private OllamaProcess _proc;
    
    #endregion

    #region Constructors

    public OllamaManager(IGpuDetectorService gpuDetector, OllamaInstaller installer, OllamaProcess proc, ILogger<OllamaManager> logger)
    {
        _gpuDetector = gpuDetector ?? throw new ArgumentNullException(nameof(gpuDetector));
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
        _proc = proc ?? throw new ArgumentNullException(nameof(proc));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Properties
    
    public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited;

    #endregion

    #region Methods

    // ---------------------------
    // INSTALLATION
    // ---------------------------

    public async Task<bool> EnsureInstalledAsync()
    {
        _ollamaPath = await _installer.EnsureInstalledAsync();
        if (_ollamaPath == null) return false;
        return true;
    }

    // ---------------------------
    // SERVER CONTROL
    // ---------------------------

    public async Task<bool> StartServerAsync()
    {
        if (IsRunning)
            return true;

        if (_ollamaPath == null)
        {
            if (!await EnsureInstalledAsync())
                return false;
        }

        try
        {
            _logger.LogInformation("Starting Ollama server…");

            var psi = new ProcessStartInfo
            {
                FileName = _ollamaPath!,
                Arguments = "serve",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _serverProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            _serverProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogInformation($"[ollama] {e.Data}");
            };
            _serverProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogError($"[ollama] {e.Data}");
            };
            _serverProcess.Exited += (_, __) =>
            {
                _logger.LogInformation("Ollama server exited.");
            };

            if (!_serverProcess.Start())
            {
                _logger.LogError("Failed to start Ollama server.");
                return false;
            }

            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            await Task.Delay(1200);

            if (!IsRunning)
            {
                _logger.LogError("Ollama server died immediately.");
                return false;
            }

            _logger.LogInformation("Ollama server started.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error starting server: {ex}");
            return false;
        }
    }

    public Task StopServerAsync()
    {
        try
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _logger.LogInformation("Stopping Ollama server…");
                _serverProcess.Kill();
                _serverProcess.WaitForExit(1500);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error stopping server: {ex}");
        }

        _serverProcess = null;
        return Task.CompletedTask;
    }

    // ---------------------------
    // OLLAMA COMMANDS VIA OllamaProcess
    // ---------------------------

    private Task<string> ExecAsync(string args, CancellationToken ct = default)
    {
        if (_proc == null)
            throw new InvalidOperationException("Ollama not installed or Manager not initialized.");

        // TODO: The "--gpu" argument appears to have been removed from the latest Ollama CLI.
        // args = string.Concat($"--gpu {_gpuVendor.GetVendorString()} ", args);

        return _proc.RunAsync(_ollamaPath ?? throw new ApplicationException("This should have been populated."), args, null, ct);
    }

    public Task<string> PullModelAsync(string name, CancellationToken ct = default) =>
        ExecAsync($"pull {name}", ct);

    public Task<string> ListModelsAsync(CancellationToken ct = default) =>
        ExecAsync("list", ct);

    public Task<string> ListRunningAsync(CancellationToken ct = default) =>
        ExecAsync("ps", ct);

    public Task<string> ShowInfoAsync(CancellationToken ct = default) =>
        ExecAsync("show", ct);

    public void Dispose()
    {
        try
        {
            if (IsRunning) _serverProcess?.Kill();
        }
        catch
		{
		}

        _serverProcess?.Dispose();
    }

    #endregion
}
