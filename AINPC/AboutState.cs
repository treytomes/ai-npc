using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AINPC;

internal class AboutState : AppState
{
	#region Fields

	private readonly ILogger<AboutState> _logger;

	#endregion

	#region Constructors

	public AboutState(IStateManager states, ILogger<AboutState> logger)
		: base(states)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

	}

	#endregion

	#region Methods

	public override async Task OnLoadAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnEnterAsync()
	{
		AnsiConsole.Clear();

		var aboutPanel = new Panel(
			new Rows(
				new FigletText("AINPC").Color(Color.Blue),
				new Rule(),
				new Markup("[yellow]Version:[/] 1.0.0"),
				new Markup("[yellow]Description:[/] Natural Language Understanding & Fuzzy Search Library"),
				new Markup(""),
				new Markup("[blue]Features:[/]"),
				new Markup("  • Intent classification for conversational AI"),
				new Markup("  • Fuzzy string matching using character vectors"),
				new Markup("  • High-performance parallel search"),
				new Markup("  • Configurable n-gram analysis"),
				new Markup(""),
				new Markup("[green]Created with Spectre.Console[/]")
			))
			.Header("[yellow]About This Application[/]")
			.Border(BoxBorder.Rounded)
			.Expand();

		AnsiConsole.Write(aboutPanel);
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("Press [blue]Enter[/] to continue...");

		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		Console.ReadLine();
		await LeaveAsync();
	}

	#endregion
}