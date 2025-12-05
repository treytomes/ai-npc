using AINPC.OllamaRuntime;
using AINPC.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

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

	public MainState(
		IOptions<AppSettings> settings,
		ILogger<MainState> logger,
		IServiceProvider serviceProvider,
		OllamaRepo ollamaRepo)
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
		AnsiConsole.Write(
			new Rule("[yellow]AINPC Ollama Test[/]")
			{
				Style = Style.Parse("yellow dim")
			});

		await _ollamaRepo.InitializeAsync();
		await _ollamaRepo.SetModelAsync(_settings.ModelId);

		AnsiConsole.MarkupLine("[green]Ollama server is running.[/]");
	}

	protected override async Task UnloadStateAsync()
	{
		_ollamaRepo.Dispose();
		AnsiConsole.MarkupLine("[red]Server stopped.[/]");
		await Task.CompletedTask;
	}

	public override async Task RunAsync()
	{
		try
		{
			await LoadStateAsync();

			var systemPrompt =
@"You are a helpful assistant.

Your first priority is to use tools when they are relevant.
Only answer from your own knowledge if no tool applies.
If you do not know something, say so.

When the user asks about the weather, ALWAYS call the GetWeather tool. Do not guess the weather.

Keep responses short and direct unless the user requests more detail.
";

			var chat = _ollamaRepo.CreateChat(systemPrompt);

			IEnumerable<object> tools =
			[
				new GetWeatherTool()
			];

			AnsiConsole.MarkupLine("[cyan]Beginning interactive chat.[/]");

			while (true)
			{
				var message = AnsiConsole.Prompt(
					new TextPrompt<string>("[bold yellow]You:[/] ")
						.AllowEmpty());

				if (string.IsNullOrWhiteSpace(message))
					break;

				AnsiConsole.Markup("[bold green]Assistant:[/] ");

				// Stream output one token at a time.
				await foreach (var token in chat.SendAsync(message, tools))
				{
					AnsiConsole.Markup(token.EscapeMarkup());
				}

				AnsiConsole.WriteLine();
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
