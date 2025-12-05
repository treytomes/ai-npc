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
			new FigletText("AINPC")
				.LeftJustified()
				.Color(Color.Cyan1));

		AnsiConsole.MarkupLine("[grey]Booting system...[/]");

		await _ollamaRepo.InitializeAsync();

		await AnsiConsole.Status()
			.StartAsync("Selecting model...", async ctx =>
			{
				await _ollamaRepo.SetModelAsync(_settings.ModelId);
			});

		AnsiConsole.MarkupLine($"[green]✔ Ollama server is running using model:[/] [yellow]{_settings.ModelId}[/]");
		AnsiConsole.WriteLine();
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

			var systemPrompt = @"You are a helpful assistant.

Your first priority is to use tools when they are relevant.
Only answer from your own knowledge if no tool applies.
If you do not know something, say so.

When the user asks about the weather, ALWAYS call the GetWeather tool. Do not guess the weather.

Keep responses short and direct unless the user requests more detail.
";

			var chat = _ollamaRepo.CreateChat(systemPrompt);

			IEnumerable<object> tools = new object[]
			{
				new GetWeatherTool()
			};

			AnsiConsole.MarkupLine("[bold]Type your message. Press ENTER on an empty line to quit.[/]\n");

			while (true)
			{
				var userMsg = AnsiConsole.Prompt(
					new TextPrompt<string>("[cyan]You:[/] ")
						.AllowEmpty());

				if (string.IsNullOrWhiteSpace(userMsg))
					break;

				AnsiConsole.Markup("[green]Assistant:[/] ");

				await foreach (var token in chat.SendAsync(userMsg, tools))
				{
					// Stream tokens directly to console
					AnsiConsole.Write(token);
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
