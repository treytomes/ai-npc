using LLM.Intent.Demo;
using Spectre.Console;

namespace LLM.Intent;

internal static class Program
{
	internal static async Task<int> Main(params string[] args)
	{
		DisplayHeader();

		while (true)
		{
			var choice = DisplayMainMenu();

			var result = choice switch
			{
				MenuChoice.ShopkeeperIntent => await ShopkeeperDemo.RunAsync(),
				MenuChoice.InteractiveFuzzySearch => await CharacterVectorDemo.RunAsync(),
				MenuChoice.FuzzySearchTests => await FuzzySearchTests.RunAsync(),
				MenuChoice.FuzzySearchExamples => await FuzzySearchDemo.RunAsync(),
				MenuChoice.About => DisplayAbout(),
				MenuChoice.Exit => -1,
				_ => 0
			};

			if (result == -1)
				break;

			AnsiConsole.WriteLine();
			if (!AnsiConsole.Confirm("Return to main menu?", true))
				break;

			AnsiConsole.Clear();
			DisplayHeader();
		}

		DisplayGoodbye();
		return 0;
	}

	private static void DisplayHeader()
	{
		AnsiConsole.Write(
			new FigletText("LLM Intent")
				.Centered()
				.Color(Color.Blue));

		AnsiConsole.Write(
			new Rule("[grey]Natural Language Understanding & Fuzzy Search[/]")
				.RuleStyle("blue"));

		AnsiConsole.WriteLine();
	}

	private static MenuChoice DisplayMainMenu()
	{
		return AnsiConsole.Prompt(
			new SelectionPrompt<MenuChoice>()
				.Title("[yellow]What would you like to do?[/]")
				.PageSize(10)
				.UseConverter(choice => choice switch
				{
					MenuChoice.ShopkeeperIntent => "🛍️  Test Shopkeeper Intent Classification",
					MenuChoice.InteractiveFuzzySearch => "🔍  Interactive Fuzzy Search",
					MenuChoice.FuzzySearchTests => "🧪  Run Fuzzy Search Tests",
					MenuChoice.FuzzySearchExamples => "📊  Fuzzy Search Examples",
					MenuChoice.About => "ℹ️  About",
					MenuChoice.Exit => "❌  Exit",
					_ => choice.ToString()
				})
				.AddChoices(Enum.GetValues<MenuChoice>()));
	}

	private static int DisplayAbout()
	{
		AnsiConsole.Clear();

		var aboutPanel = new Panel(
			new Rows(
				new FigletText("LLM Intent").Color(Color.Blue),
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
		Console.ReadLine();
		return 0;
	}

	private static void DisplayGoodbye()
	{
		AnsiConsole.Clear();

		var goodbyeText = new FigletText("Goodbye!")
			.Centered()
			.Color(Color.Green);

		AnsiConsole.Write(goodbyeText);
		AnsiConsole.MarkupLine("[grey]Thank you for using LLM Intent![/]");

		Thread.Sleep(1500);
	}

	private enum MenuChoice
	{
		ShopkeeperIntent,
		InteractiveFuzzySearch,
		FuzzySearchTests,
		FuzzySearchExamples,
		About,
		Exit
	}
}