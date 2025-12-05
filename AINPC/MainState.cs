using AINPC.OllamaRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AINPC;

class MainState : AppState
{
	#region Fields

	private readonly AppSettings _settings;
	private readonly ILogger<MainState> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly OllamaRepo _ollamaRepo;

	#endregion

	#region Constructors

	public MainState(IOptions<AppSettings> settings, ILogger<MainState> logger, IServiceProvider serviceProvider, OllamaRepo ollamaRepo)
	{
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));
	}

	#endregion

	#region Methods

	protected override async Task LoadStateAsync()
	{
		Console.WriteLine("AINPC Ollama Test");
		Console.WriteLine("----------------------------------");

		await _ollamaRepo.InitializeAsync();
		await _ollamaRepo.SetModelAsync(_settings.ModelId);

		Console.WriteLine("Ollama server is running.");
	}

	protected override async Task UnloadStateAsync()
	{
		// Stop server cleanly.
		_ollamaRepo.Dispose();
		Console.WriteLine("Server stopped.");
		await Task.CompletedTask;
	}

	public override async Task RunAsync()
	{
		try
		{
			await LoadStateAsync();

			// await _ollamaRepo.ReportInstalledModelsAsync();
			// await _ollamaRepo.ReportRunningModelsAsync();

			// Basic text generation.
			// Console.WriteLine("Testing: `My name is Trey. How are you today?`");
			// var response = await _ollamaRepo.GenerateAsync("How are you today?");
			// Console.WriteLine(response);

			// Interactive chat.
			var chat = _ollamaRepo.CreateChat();

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
				{
					Console.Write(answerToken);
				}

				Console.WriteLine();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, ex.Message);
			throw;
		}
		finally
		{
			await UnloadStateAsync();
		}
	}

	#endregion
}