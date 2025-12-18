using Spectre.Console;

namespace LLM.Intent.Demo;

/// <summary>
/// Provides consistent UI elements and styling for the application.
/// </summary>
internal static class ConsoleUI
{
	public static class Colors
	{
		public static Color Primary => Color.Blue;
		public static Color Secondary => Color.Yellow;
		public static Color Success => Color.Green;
		public static Color Error => Color.Red;
		public static Color Warning => Color.Orange1;
		public static Color Info => Color.Cyan1;
		public static Color Muted => Color.Grey;
	}

	/// <summary>
	/// Creates a styled header rule.
	/// </summary>
	public static Rule CreateHeader(string title)
	{
		return new Rule($"[{Colors.Secondary}]{title}[/]")
			.RuleStyle(Colors.Muted.ToString())
			.LeftJustified();
	}

	/// <summary>
	/// Creates a consistent panel style.
	/// </summary>
	public static Panel CreatePanel(string content, string header)
	{
		return new Panel(content)
			.Header($"[{Colors.Secondary}]{header}[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Colors.Primary);
	}

	/// <summary>
	/// Shows a progress spinner with a task.
	/// </summary>
	public static async Task<T> ShowSpinner<T>(string status, Func<Task<T>> task)
	{
		T result = default!;

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.SpinnerStyle(Style.Parse(Colors.Primary.ToString()))
			.StartAsync(status, async ctx =>
			{
				result = await task();
			});

		return result;
	}

	/// <summary>
	/// Displays a success message.
	/// </summary>
	public static void Success(string message)
	{
		AnsiConsole.MarkupLine($"[{Colors.Success}]✓[/] {message}");
	}

	/// <summary>
	/// Displays an error message.
	/// </summary>
	public static void Error(string message)
	{
		AnsiConsole.MarkupLine($"[{Colors.Error}]✗[/] {message}");
	}

	/// <summary>
	/// Displays a warning message.
	/// </summary>
	public static void Warning(string message)
	{
		AnsiConsole.MarkupLine($"[{Colors.Warning}]⚠[/] {message}");
	}

	/// <summary>
	/// Displays an info message.
	/// </summary>
	public static void Info(string message)
	{
		AnsiConsole.MarkupLine($"[{Colors.Info}]ℹ[/] {message}");
	}

	/// <summary>
	/// Creates a consistent table style.
	/// </summary>
	public static Table CreateTable(string title, params string[] columns)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Colors.Primary)
			.Title($"[{Colors.Secondary}]{title}[/]");

		foreach (var column in columns)
		{
			table.AddColumn(column);
		}

		return table;
	}

	/// <summary>
	/// Prompts for confirmation with consistent styling.
	/// </summary>
	public static bool Confirm(string question, bool defaultValue = true)
	{
		return AnsiConsole.Confirm($"[{Colors.Info}]{question}[/]", defaultValue);
	}

	/// <summary>
	/// Creates a progress bar configuration.
	/// </summary>
	public static Progress CreateProgressBar()
	{
		return AnsiConsole.Progress()
			.AutoClear(false)
			.HideCompleted(false)
			.Columns(
			[
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn(),
				new RemainingTimeColumn(),
				new SpinnerColumn()
			]);
	}
}