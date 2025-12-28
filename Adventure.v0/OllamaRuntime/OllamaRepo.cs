using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Adventure.OllamaRuntime;

class OllamaRepo : IDisposable
{
	#region Fields

	private readonly ILogger<OllamaRepo> _logger;
	private readonly Uri _serverUri;
	private readonly OllamaManager _manager;
	private OllamaApiClient? _client;
	private bool _disposedValue = false;

	#endregion

	#region Constructors

	public OllamaRepo(IOptions<AppSettings> settings, ILogger<OllamaRepo> logger, OllamaManager manager)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_serverUri = new Uri(settings.Value?.OllamaUrl ?? throw new ArgumentNullException(nameof(settings)));
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	#endregion

	#region Methods

	public async Task InitializeAsync()
	{
		// Ensure Ollama exists.
		if (!await _manager.EnsureInstalledAsync())
		{
			_logger.LogError("Installation failed.");
			return;
		}

		// Start server.
		if (!await _manager.StartServerAsync())
		{
			_logger.LogError("Failed to start Ollama server.");
			return;
		}


		// The client should not be initialized until we are certain the server is running.
		_client = new OllamaApiClient(_serverUri);
	}

	/// <summary>
	/// Pull and select the model.
	/// </summary>
	public async Task SetModelAsync(string modelName)
	{
		if (_client == null) throw new ApplicationException("Ollama client is not initialized.");


		// Only pull the model if it's not already available.
		var models = await _client.ListLocalModelsAsync();
		if (!models.Any(x => x.Name == modelName))
		{
			// Pull the model, just in case it hasn't been already.
			_logger.LogInformation($"Pulling {modelName}.");
			await foreach (var status in _client.PullModelAsync(modelName))
			{
				_logger.LogTrace($"{status?.Percent}% {status?.Status}");
			}
		}

		_client.SelectedModel = modelName;
	}

	// TODO: The Report* methods don't belong here.

	/// <summary>
	/// Report installed models to the console.
	/// </summary>
	public async Task ReportInstalledModelsAsync(Action<string> report)
	{
		if (_client == null) throw new ApplicationException("Ollama client is not initialized.");

		report("Installed models:");
		var models = await _client.ListLocalModelsAsync();
		foreach (var model in models)
		{
			report($"{model.Name} - Modified: {model.ModifiedAt}");
		}
	}

	/// <summary>
	/// Report running models to the console.
	/// </summary>
	public async Task ReportRunningModelsAsync(Action<string> report)
	{
		if (_client == null) throw new ApplicationException("Ollama client is not initialized.");

		report("Running models:");
		var running = await _client.ListRunningModelsAsync();
		foreach (var model in running)
		{
			report(model.Name);
		}
	}

	public async Task<string> GenerateAsync(string prompt, ConversationContext? context = null, CancellationToken cancellationToken = default)
	{
		if (_client == null) throw new ApplicationException("Ollama client is not initialized.");

		var sb = new StringBuilder();
		await foreach (var stream in _client.GenerateAsync(prompt, context, cancellationToken))
		{
			if (stream == null) continue;
			sb.Append(stream?.Response);
		}

		return sb.ToString();
	}

	public Chat CreateChat(string? systemPrompt = null)
	{
		if (_client == null) throw new ApplicationException("Ollama client is not initialized.");

		if (string.IsNullOrWhiteSpace(systemPrompt))
		{
			return new Chat(_client);
		}
		return new Chat(_client, systemPrompt);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_manager.Dispose();
			}

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}