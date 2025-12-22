using AINPC.CatalystRuntime;
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
	private readonly CatalystManager _catalyst;

	#endregion

	#region Constructors

	public OllamaAppEngine(IOptions<AppSettings> settings, IServiceProvider serviceProvider, ILogger<OllamaAppEngine> logger, OllamaRepo ollamaRepo, CatalystManager catalyst)
		: base(serviceProvider, logger)
	{
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));
		_catalyst = catalyst ?? throw new ArgumentNullException(nameof(catalyst));
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

		await AnsiConsole.Status()
			.StartAsync("Initializing Catalyst...", async ctx =>
			{
				await _catalyst.InitializeAsync();
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