using Adventure.LLM.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Adventure;

class LlmAppEngine : AppEngine
{
	#region Fields

	private ILogger<LlmAppEngine> _logger;
	private readonly LLM.REPL.AppSettings _settings;
	private readonly ILlmManager _llmManager;

	#endregion

	#region Constructors

	public LlmAppEngine(IOptions<LLM.REPL.AppSettings> settings, IServiceProvider serviceProvider, ILogger<LlmAppEngine> logger, ILlmManager llmManager)
		: base(serviceProvider, logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
		_llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
	}

	#endregion

	#region Methods

	protected override async Task InitializeAsync()
	{
		AnsiConsole.Write(
			new FigletText("Adventure.LLM")
				.LeftJustified()
				.Color(Color.Cyan1));

		AnsiConsole.MarkupLine("[grey]Booting system...[/]");

		await _llmManager.InitializeAsync();
		_llmManager.SetModel(_settings.ModelId);

		AnsiConsole.MarkupLine($"[green]✔ Ollama server is running using model:[/] [yellow]{_settings.ModelId}[/]");
		AnsiConsole.WriteLine();
		Thread.Sleep(500);
	}

	protected override async Task DestroyAsync()
	{
		_llmManager.Dispose();
		AnsiConsole.MarkupLine("[red]Server stopped.[/]");
		await Task.CompletedTask;
	}

	#endregion
}