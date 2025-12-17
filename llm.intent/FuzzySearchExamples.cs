using LLM.Intent.FuzzySearch;
using Spectre.Console;
using System.Diagnostics;

namespace LLM.Intent;

/// <summary>
/// Example usage and additional test scenarios with Spectre.Console visualization.
/// </summary>
internal static class FuzzySearchExamples
{
	public static async Task<int> RunAsync()
	{
		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which example would you like to run?")
				.AddChoices(new[]
				{
					"📝 Basic Fuzzy Search",
					"⚙️ Custom Configuration",
					"🚀 Performance Test",
					"🔙 Back to Main Menu"
				}));

		return choice switch
		{
			"📝 Basic Fuzzy Search" => await BasicExample(),
			"⚙️ Custom Configuration" => await CustomConfigurationExample(),
			"🚀 Performance Test" => await PerformanceExample(),
			_ => 0
		};
	}

	/// <summary>
	/// Demonstrates basic fuzzy search usage.
	/// </summary>
	public static async Task<int> BasicExample()
	{
		AnsiConsole.Write(
			new Rule("[yellow]Basic Fuzzy Search Example[/]")
				.RuleStyle("grey")
				.LeftJustified());

		var items = new[]
		{
			"Microsoft Word",
			"Microsoft Excel",
			"Microsoft PowerPoint",
			"Adobe Photoshop",
			"Adobe Illustrator",
			"Visual Studio Code",
			"Google Chrome",
			"Mozilla Firefox"
		};

		var searchEngine = new FuzzySearchEngine(items);

		// Display available items
		var itemsPanel = new Panel(string.Join("\n", items))
			.Header("[blue]Available Items[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(itemsPanel);

		var query = AnsiConsole.Ask<string>("Enter search term (try with typos like 'mocrosoft'): ");

		await AnsiConsole.Progress()
			.AutoClear(false)
			.Columns(new ProgressColumn[]
			{
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn(),
				new SpinnerColumn(),
			})
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask("[green]Searching...[/]", maxValue: 100);

				var results = await searchEngine.SearchAsync(query);
				task.Value = 100;

				// Display results
				if (!results.Any())
				{
					AnsiConsole.MarkupLine("[red]No results found![/]");
				}
				else
				{
					var table = new Table()
						.Border(TableBorder.Rounded)
						.Title($"[yellow]Search Results for '{query}'[/]")
						.AddColumn("Rank")
						.AddColumn("Item")
						.AddColumn("Score")
						.AddColumn("Match Quality");

					int rank = 1;
					foreach (var result in results.Take(5))
					{
						var quality = result.Score switch
						{
							>= 0.9 => "[green]Excellent[/]",
							>= 0.7 => "[yellow]Good[/]",
							>= 0.5 => "[orange1]Fair[/]",
							_ => "[red]Poor[/]"
						};

						table.AddRow(
							rank.ToString(),
							result.Text,
							$"{result.Score:P0}",
							quality);
						rank++;
					}

					AnsiConsole.Write(table);
				}
			});

		return 0;
	}

	/// <summary>
	/// Demonstrates custom configuration options.
	/// </summary>
	public static async Task<int> CustomConfigurationExample()
	{
		AnsiConsole.Write(
			new Rule("[yellow]Custom Configuration Example[/]")
				.RuleStyle("grey")
				.LeftJustified());

		var files = new[]
		{
			"document.txt",
			"documentation.pdf",
			"docker-compose.yml",
			"Dockerfile",
			"package.json",
			"package-lock.json"
		};

		// Show configuration options
		var configPanel = new Panel(
			"[blue]Configuration:[/]\n" +
			"• Min N-gram Size: 2\n" +
			"• Max N-gram Size: 4\n" +
			"• Include Word N-grams: False\n" +
			"• Minimum Similarity: 30%")
			.Header("[yellow]Search Options[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(configPanel);

		var options = new SearchOptions
		{
			MinNgramSize = 2,
			MaxNgramSize = 4,
			IncludeWordNgrams = false,
			MinimumSimilarity = 0.3
		};

		var searchEngine = new FuzzySearchEngine(files, options);

		var query = AnsiConsole.Ask<string>("Enter search term (try 'doc'): ");

		var results = await searchEngine.SearchAsync(query);

		var resultsTable = new Table()
			.Border(TableBorder.Simple)
			.AddColumn("File")
			.AddColumn("Score");

		foreach (var result in results)
		{
			resultsTable.AddRow(result.Text, $"{result.Score:P0}");
		}

		AnsiConsole.Write(resultsTable);
		AnsiConsole.MarkupLine($"\n[green]Found {results.Count()} results for '{query}'[/]");

		return 0;
	}

	/// <summary>
	/// Demonstrates performance with large datasets.
	/// </summary>
	public static async Task<int> PerformanceExample()
	{
		AnsiConsole.Write(
			new Rule("[yellow]Performance Test[/]")
				.RuleStyle("grey")
				.LeftJustified());

		var itemCount = AnsiConsole.Prompt(
			new TextPrompt<int>("How many items to generate?")
				.DefaultValue(10000)
				.ValidationErrorMessage("[red]Please enter a valid number[/]")
				.Validate(n => n > 0 && n <= 100000));

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync($"Generating {itemCount:N0} items...", async ctx =>
			{
				// Generate a large dataset
				var items = Enumerable.Range(1, itemCount)
					.Select(i => $"Item_{i:D5}_Description_With_Some_Text_{Guid.NewGuid():N}")
					.ToList();

				ctx.Status($"Creating search engine with {itemCount:N0} items...");
				var searchEngine = new FuzzySearchEngine(items);

				ctx.Status("Ready to search...");
				await Task.Delay(500); // Brief pause for visibility
			});

		var searchTerm = AnsiConsole.Ask<string>("Enter search term (e.g., 'Item_00500'): ");

		var stopwatch = new Stopwatch();

		await AnsiConsole.Live(GeneratePerformanceDisplay(stopwatch, 0, 0))
			.StartAsync(async ctx =>
			{
				stopwatch.Start();
				var searchEngine = new FuzzySearchEngine(
					Enumerable.Range(1, itemCount)
						.Select(i => $"Item_{i:D5}_Description_With_Some_Text_{Guid.NewGuid():N}")
						.ToList());

				var results = await searchEngine.SearchAsync(searchTerm);
				stopwatch.Stop();

				ctx.UpdateTarget(GeneratePerformanceDisplay(stopwatch, results.Count(), itemCount));

				await Task.Delay(1000); // Show final results

				// Display top results
				if (results.Any())
				{
					var table = new Table()
						.Border(TableBorder.Rounded)
						.Title("[yellow]Top 5 Results[/]")
						.AddColumn("Item")
						.AddColumn("Score");

					foreach (var result in results.Take(5))
					{
						table.AddRow(
							result.Text.Length > 50 ? result.Text[..47] + "..." : result.Text,
							$"{result.Score:P0}");
					}

					AnsiConsole.Write(table);
				}
			});

		return 0;
	}

	private static Panel GeneratePerformanceDisplay(Stopwatch stopwatch, int resultCount, int totalItems)
	{
		var content = new Rows(
			new Markup($"[blue]Search Time:[/] {stopwatch.ElapsedMilliseconds}ms"),
			new Markup($"[blue]Items Searched:[/] {totalItems:N0}"),
			new Markup($"[blue]Results Found:[/] {resultCount:N0}"),
			new Markup($"[blue]Performance:[/] {(totalItems > 0 ? totalItems / Math.Max(1, stopwatch.ElapsedMilliseconds) : 0):N0} items/ms")
		);

		return new Panel(content)
			.Header("[yellow]Performance Metrics[/]")
			.Border(BoxBorder.Rounded);
	}
}