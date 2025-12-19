using AINPC.Renderables;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AINPC.States;

internal class MainMenuState : AppState
{
	#region Fields

	private readonly ILogger<MainMenuState> _logger;

	#endregion

	#region Constructors

	public MainMenuState(IStateManager states, ILogger<MainMenuState> logger)
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
		AnsiConsole.Clear();

		var goodbyeText = new FigletText("Goodbye!")
			.Centered()
			.Color(Color.Green);

		AnsiConsole.Write(goodbyeText);
		AnsiConsole.MarkupLine("[grey]Thanks for playing!![/]");

		await Task.CompletedTask;
	}

	public override async Task OnEnterAsync()
	{
		AnsiConsole.Clear();
		DisplayHeader();
		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		var choice = DisplayMainMenu();

		switch (choice)
		{
			case MenuChoice.Chat:
				await EnterStateAsync<ChatState>();
				break;
			case MenuChoice.ShopkeeperIntent:
				await EnterStateAsync<ShopkeeperIntentState>();
				break;
			case MenuChoice.InteractiveFuzzySearch:
				await EnterStateAsync<InteractiveFuzzySearchState>();
				break;
			case MenuChoice.FuzzySearchExamples:
				await EnterStateAsync<FuzzySearchExamplesState>();
				break;
			case MenuChoice.FuzzySearchTests:
				await EnterStateAsync<FuzzySearchTestState>();
				break;
			case MenuChoice.About:
				await EnterStateAsync<AboutState>();
				break;
			case MenuChoice.Exit:
				await LeaveAsync();
				break;
		}
	}

	private void DisplayHeader()
	{
		new HeaderRenderable("AINPC Demo", "Natural Language Understanding & Fuzzy Search").Render();
	}

	private MenuChoice DisplayMainMenu()
	{
		return AnsiConsole.Prompt(
			new SelectionPrompt<MenuChoice>()
				.Title("[yellow]What would you like to do?[/]")
				.PageSize(10)
				.UseConverter(choice => choice switch
				{
					MenuChoice.Chat => "💬 Adventure System Chat",
					MenuChoice.ShopkeeperIntent => "🛍️  Test Shopkeeper Intent Classification",
					MenuChoice.InteractiveFuzzySearch => "🔍 Interactive Fuzzy Search",
					MenuChoice.FuzzySearchExamples => "📊 Fuzzy Search Examples",
					MenuChoice.FuzzySearchTests => "🧪 Run Fuzzy Search Tests",
					MenuChoice.About => "ℹ️  About",
					MenuChoice.Exit => "❌ Exit",
					_ => choice.ToString()
				})
				.AddChoices(Enum.GetValues<MenuChoice>()));
	}

	#endregion

	private enum MenuChoice
	{
		Chat,
		ShopkeeperIntent,
		InteractiveFuzzySearch,
		FuzzySearchTests,
		FuzzySearchExamples,
		About,
		Exit
	}
}