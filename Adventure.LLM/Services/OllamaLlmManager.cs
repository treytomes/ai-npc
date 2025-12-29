using Adventure.LLM.Ollama;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;

namespace Adventure.LLM.Services;

internal sealed class OllamaLlmManager : ILlmManager
{
	#region Fields

	private readonly ILogger<OllamaLlmManager> _logger;
	private readonly Uri _serverUri;
	private readonly OllamaProcessManager _manager;
	private bool _disposedValue = false;
	private string _selectedModelName = string.Empty;

	#endregion

	#region Constructors

	public OllamaLlmManager(OllamaProps props, ILogger<OllamaLlmManager> logger, OllamaProcessManager manager)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_serverUri = new Uri(props.Url ?? throw new ArgumentNullException(nameof(props)));
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	#endregion

	#region Methods

	public async Task<IChatClient> CreateChatClient()
	{
		// The client should not be initialized until we are certain the server is running.
		var client = new OllamaApiClient(_serverUri);
		if (client == null) throw new ApplicationException("Ollama client is not initialized.");

		// Only pull the model if it's not already available.
		var models = await client.ListLocalModelsAsync();
		if (!models.Any(x => x.Name == _selectedModelName))
		{
			// Pull the model, just in case it hasn't been already.
			_logger.LogInformation($"Pulling {_selectedModelName}.");
			await foreach (var status in client.PullModelAsync(_selectedModelName))
			{
				_logger.LogTrace($"{status?.Percent}% {status?.Status}");
			}
		}

		client.SelectedModel = _selectedModelName;
		return client;
	}

	public async Task InitializeAsync()
	{
		if (_manager.IsRunning)
		{
			return;
		}

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
	}

	/// <summary>
	/// Pull and select the model.
	/// </summary>
	public void SetModel(string modelName)
	{
		_selectedModelName = modelName;
	}

	private void Dispose(bool disposing)
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