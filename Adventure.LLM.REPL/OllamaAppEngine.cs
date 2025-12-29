using Adventure.LLM.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Adventure;

class OllamaAppEngine : AppEngine
{
	#region Fields

	private readonly LLM.REPL.AppSettings _settings;
	private readonly ILLMManager _llmManager;

	#endregion

	#region Constructors

	public OllamaAppEngine(IOptions<LLM.REPL.AppSettings> settings, IServiceProvider serviceProvider, ILogger<OllamaAppEngine> logger, ILLMManager llmManager)
		: base(serviceProvider, logger)
	{
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

		await AnsiConsole.Status()
			.StartAsync("Selecting model...", async ctx =>
			{
				await _llmManager.SetModelAsync(_settings.ModelId);
			});

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