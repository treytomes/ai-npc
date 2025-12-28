using Adventure.Intent.FuzzySearch;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;

namespace Adventure.States;

/// <summary>
/// Runs an interactive test of the fuzzy search functionality using Windows system files.
/// </summary>
internal class FuzzySearchExamplesState : AppState
{
	#region Constants

	#endregion

	#region Fields

	private readonly ILogger<FuzzySearchExamplesState> _logger;

	#endregion

	#region Constructors

	public FuzzySearchExamplesState(IStateManager states, ILogger<FuzzySearchExamplesState> logger)
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
		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which example would you like to run?")
				.AddChoices(
				[
					"📝 Basic Fuzzy Search",
					"⚙️ Custom Configuration",
					"🚀 Performance Test",
					"🔙 Back to Main Menu"
				]));

		try
		{
			await (choice switch
			{
				"📝 Basic Fuzzy Search" => BasicExample(),
				"⚙️ Custom Configuration" => CustomConfigurationExample(),
				"🚀 Performance Test" => PerformanceExample(),
				_ => LeaveAsync(),
			});
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
		}
	}

	/// <summary>
	/// Demonstrates basic fuzzy search usage.
	/// </summary>
	public async Task BasicExample()
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
		var itemsPanel = new Panel(string.Join("\n", items.Select((item, i) => $"{i}. {item}")))
			.Header("[blue]Available Items[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(itemsPanel);

		var query = AnsiConsole.Ask<string>("Enter search term (try with typos like 'mocrosoft' or an ID like '2'): ");

		// Simple status instead of progress bar
		var results = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync("Searching...", async ctx => await searchEngine.SearchAsync(query));

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
	}

	/// <summary>
	/// Demonstrates custom configuration options.
	/// </summary>
	public async Task CustomConfigurationExample()
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
			"package-lock.json",
			"README.md",
			"readme.txt"
		};

		// Show files
		AnsiConsole.MarkupLine("[blue]Available files:[/]");
		foreach (var file in files)
		{
			AnsiConsole.MarkupLine($"  • {file}");
		}
		AnsiConsole.WriteLine();

		var options = new SearchOptions
		{
			MinNgramSize = 2,
			MaxNgramSize = 4,
			IncludeWordNgrams = false,
			MinimumSimilarity = 0.3
		};

		// Show configuration
		var configTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Search Configuration[/]")
			.AddColumn("Setting")
			.AddColumn("Value");

		configTable.AddRow("Min N-gram Size", options.MinNgramSize.ToString());
		configTable.AddRow("Max N-gram Size", options.MaxNgramSize.ToString());
		configTable.AddRow("Include Word N-grams", options.IncludeWordNgrams.ToString());
		configTable.AddRow("Minimum Similarity", $"{options.MinimumSimilarity:P0}");

		AnsiConsole.Write(configTable);

		var searchEngine = new FuzzySearchEngine(files, options);

		var query = AnsiConsole.Ask<string>("Enter search term (try 'doc' or 'read'): ");

		var results = await searchEngine.SearchAsync(query);

		if (!results.Any())
		{
			AnsiConsole.MarkupLine("[red]No results found![/]");
		}
		else
		{
			var resultsTable = new Table()
				.Border(TableBorder.Simple)
				.Title($"[green]Found {results.Count()} results for '{query}'[/]")
				.AddColumn("File")
				.AddColumn("Score")
				.AddColumn("Visual");

			foreach (var result in results)
			{
				var bar = new BarChart()
					.Width(20)
					.AddItem(string.Empty, result.Score * 100, Color.Green);

				resultsTable.AddRow(
					new Text(result.Text),
					new Text($"{result.Score:P0}"),
					bar);
			}

			AnsiConsole.Write(resultsTable);
		}
	}

	/// <summary>
	/// Demonstrates performance with large datasets.
	/// </summary>
	public async Task PerformanceExample()
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

		// Generate dataset once
		List<string> items = null!;
		var generationTime = await MeasureTime(async () =>
		{
			await Task.Run(() =>
			{
				items = Enumerable.Range(1, itemCount)
					.Select(i => $"Item_{i:D5}_Description_With_Some_Text_{i:X8}")
					.ToList();
			});
		});

		AnsiConsole.MarkupLine($"[green]✓[/] Generated {itemCount:N0} items in {generationTime}ms");

		// Create search engine
		FuzzySearchEngine searchEngine = null!;
		var engineCreationTime = await MeasureTime(async () =>
		{
			await Task.Run(() => searchEngine = new FuzzySearchEngine(items));
		});

		AnsiConsole.MarkupLine($"[green]✓[/] Created search engine in {engineCreationTime}ms");
		AnsiConsole.WriteLine();

		var searchTerm = AnsiConsole.Ask<string>("Enter search term (e.g., 'Item_00500'): ");

		// Warm-up run
		await searchEngine.SearchAsync(searchTerm);

		// Perform multiple search runs for better accuracy
		const int runs = 5;
		var searchTimes = new List<long>();
		IEnumerable<ISearchResult> results = null!;

		AnsiConsole.MarkupLine($"[yellow]Performing {runs} search runs...[/]");

		for (int i = 0; i < runs; i++)
		{
			var time = await MeasureTime(async () =>
			{
				results = await searchEngine.SearchAsync(searchTerm);
			});
			searchTimes.Add(time);
			AnsiConsole.MarkupLine($"  Run {i + 1}: {time}ms");
		}

		// Display performance metrics
		var metricsPanel = new Panel(
			new Rows(
				new Markup($"[blue]Dataset Size:[/] {itemCount:N0} items"),
				new Markup($"[blue]Search Term:[/] {searchTerm}"),
				new Markup($"[blue]Results Found:[/] {results?.Count() ?? 0}"),
				new Rule(),
				new Markup($"[blue]Average Search Time:[/] {searchTimes.Average():F2}ms"),
				new Markup($"[blue]Min Search Time:[/] {searchTimes.Min()}ms"),
				new Markup($"[blue]Max Search Time:[/] {searchTimes.Max()}ms"),
				new Markup($"[blue]Search Rate:[/] ~{(1000.0 / searchTimes.Average()):F0} searches/second")
			))
			.Header("[yellow]Performance Metrics[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(metricsPanel);

		// Display top results
		if (results?.Any() == true)
		{
			AnsiConsole.WriteLine();
			var table = new Table()
				.Border(TableBorder.Rounded)
				.Title("[yellow]Top 5 Results[/]")
				.AddColumn("Rank")
				.AddColumn("Item")
				.AddColumn("Score");

			int rank = 1;
			foreach (var result in results.Take(5))
			{
				var displayText = result.Text.Length > 50
					? result.Text[..47] + "..."
					: result.Text;

				table.AddRow(
					rank.ToString(),
					displayText,
					$"{result.Score:P0}");
				rank++;
			}

			AnsiConsole.Write(table);
		}
	}

	private async Task<long> MeasureTime(Func<Task> action)
	{
		var stopwatch = Stopwatch.StartNew();
		await action();
		stopwatch.Stop();
		return stopwatch.ElapsedMilliseconds;
	}

	#endregion
}