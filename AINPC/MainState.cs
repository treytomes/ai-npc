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

Use tools only when they are the best way to answer the user's request.
Do not look for excuses to call a tool. Only call one when:
- the user asks for information that the tool directly provides, or
- the user explicitly requests the tool.

For weather questions, call the GetWeather tool only when the user
is clearly asking for actual weather information (current or forecast).
If the user is speaking metaphorically or casually, do not call the tool.

When no tool is appropriate, answer from your own knowledge.
If you do not know something, say so briefly and continue.

Keep responses short and direct unless the user asks for more detail.
Avoid repeating the same information unless the user requests it.

If the user asks you to think, reflect, explain, or discuss ideas,
respond normally—tools are not needed for general conversation.
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
