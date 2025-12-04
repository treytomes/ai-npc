using Microsoft.Extensions.Logging;
using OllamaSharp;

namespace AINPC;

class MainState : AppState
{
	#region Constants

	private const string OLLAMA_URL = "http://localhost:11434";
	private const string LANGUAGE_MODEL = "qwen2.5:0.5b";

	#endregion

	#region Fields

	private readonly ILogger<MainState> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly OllamaManager _ollamaManager;

	#endregion

	#region Constructors

	public MainState(ILogger<MainState> logger, IServiceProvider serviceProvider, OllamaManager ollamaManager)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_ollamaManager = ollamaManager ?? throw new ArgumentNullException(nameof(ollamaManager));
	}

	#endregion

	#region Methods

	public override async Task RunAsync()
	{
		Console.WriteLine("AINPC Ollama Bootstrap Test");
		Console.WriteLine("----------------------------------");

		// Ensure Ollama exists.
		if (!await _ollamaManager.EnsureInstalledAsync())
		{
			Console.WriteLine("Installation failed.");
			return;
		}

		// Start server.
		if (!await _ollamaManager.StartServerAsync())
		{
			Console.WriteLine("Failed to start Ollama server.");
			return;
		}

		Console.WriteLine("Ollama server is running.");

		// Set up the client, pointing to our Ollama server.
		var uri = new Uri(OLLAMA_URL);
		var ollama = new OllamaApiClient(uri);

		// Select a model to use for operations.
		ollama.SelectedModel = LANGUAGE_MODEL;

		// Pull a model.
		Console.WriteLine($"Pulling {LANGUAGE_MODEL}...");
		await foreach (var status in ollama.PullModelAsync(LANGUAGE_MODEL))
			Console.WriteLine($"{status?.Percent}% {status?.Status}");

		// List installed models.
		Console.WriteLine("Installed models:");
		var models = await ollama.ListLocalModelsAsync();
		foreach (var model in models)
		{
			Console.WriteLine($"{model.Name} - Modified: {model.ModifiedAt}");
		}

		// List running models.
		Console.WriteLine("Running models:");
		var running = await ollama.ListRunningModelsAsync();
		foreach (var model in running)
		{
			Console.WriteLine(model.Name);
		}

		// Basic text generation.
		Console.WriteLine("Testing: `How are you today?`");
		await foreach (var stream in ollama.GenerateAsync("How are you today?"))
			Console.Write(stream?.Response);
		Console.WriteLine(); // Add an EOL once the generator is done.

		// Interactive chat.
		var chat = new Chat(ollama);

		Console.WriteLine("Beginning interactive chat.");
		while (true)
		{
			Console.Write("You: ");
			var message = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(message))
			{
				break;
			}

			Console.Write("Assistant: ");
			await foreach (var answerToken in chat.SendAsync(message))
				Console.Write(answerToken);

			Console.WriteLine();
		}

		// Stop server cleanly.
		await _ollamaManager.StopServerAsync();
		Console.WriteLine("Server stopped.");
	}

	#endregion
}