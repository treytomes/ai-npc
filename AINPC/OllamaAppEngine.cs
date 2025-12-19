using AINPC.OllamaRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AINPC;

class OllamaAppEngine : AppEngine
{
	#region Fields

	private readonly AppSettings _settings;
	private readonly OllamaRepo _ollamaRepo;

	#endregion

	#region Constructors

	public OllamaAppEngine(IOptions<AppSettings> settings, IServiceProvider serviceProvider, ILogger<OllamaAppEngine> logger, OllamaRepo ollamaRepo)
		: base(serviceProvider, logger)
	{
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));
	}

	#endregion

	#region Methods

	protected override async Task InitializeAsync()
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
		Thread.Sleep(500);
	}

	protected override async Task DestroyAsync()
	{
		_ollamaRepo.Dispose();
		AnsiConsole.MarkupLine("[red]Server stopped.[/]");
		await Task.CompletedTask;
	}

	#endregion
}