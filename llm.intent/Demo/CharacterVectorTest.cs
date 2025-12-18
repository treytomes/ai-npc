using LLM.Intent.FuzzySearch;
using Spectre.Console;

namespace LLM.Intent.Demo;

/// <summary>
/// Provides an interactive test harness for the character vector-based fuzzy search functionality.
/// </summary>
internal static class CharacterVectorTest
{
	/// <summary>
	/// Runs an interactive test of the fuzzy search functionality using Windows system files.
	/// </summary>
	public static async Task<int> RunAsync()
	{
		AnsiConsole.Write(
			new FigletText("Fuzzy Search")
				.LeftJustified()
				.Color(Color.Cyan1));

		var useWindowsFiles = AnsiConsole.Confirm("Load files from C:\\Windows?", false);

		List<string> files;

		if (useWindowsFiles)
		{
			files = await LoadWindowsFiles();
		}
		else
		{
			files = await LoadSampleFiles();
		}

		if (!files.Any())
		{
			AnsiConsole.MarkupLine("[red]No files loaded. Exiting.[/]");
			return 1;
		}

		var options = await ConfigureSearchOptions();
		var searcher = new FuzzySearchEngine(files, options);

		await RunInteractiveSearch(searcher, files.Count);

		return 0;
	}

	private static async Task<List<string>> LoadWindowsFiles()
	{
		var files = new List<string>();

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync("Loading files from C:\\Windows...", async ctx =>
			{
				try
				{
					files = await Task.Run(() => Directory.GetFiles("c:\\windows")
						.Select(Path.GetFileName)
						.Where(f => !string.IsNullOrEmpty(f))
						.Select(x => x!)
						.ToList());
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[red]Error loading files: {ex.Message}[/]");
				}
			});

		return files;
	}

	private static Task<List<string>> LoadSampleFiles()
	{
		var sampleFiles = new List<string>
		{
			"explorer.exe",
			"notepad.exe",
			"regedit.exe",
			"system.ini",
			"win.ini",
			"WindowsUpdate.log",
			"setupact.log",
			"setuperr.log",
			"notepad.exe.mui",
			"explorer.exe.config",
			"Microsoft.Windows.Common-Controls.manifest",
			"Professional.xml",
			"Enterprise.xml",
			"Education.xml"
		};

		return Task.FromResult(sampleFiles);
	}

	private static async Task<SearchOptions> ConfigureSearchOptions()
	{
		var useDefaults = AnsiConsole.Confirm("Use default search options?", true);

		if (useDefaults)
		{
			return new SearchOptions();
		}

		var minNgramSize = AnsiConsole.Prompt(
			new TextPrompt<int>("Minimum n-gram size:")
				.DefaultValue(1)
				.ValidationErrorMessage("[red]Must be at least 1[/]")
				.Validate(n => n >= 1));

		var maxNgramSize = AnsiConsole.Prompt(
			new TextPrompt<int>("Maximum n-gram size:")
				.DefaultValue(3)
				.ValidationErrorMessage("[red]Must be greater than minimum[/]")
				.Validate(n => n >= minNgramSize));

		var includeWordNgrams = AnsiConsole.Confirm("Include word n-grams?", true);

		var minimumSimilarity = AnsiConsole.Prompt(
			new TextPrompt<double>("Minimum similarity (0-1):")
				.DefaultValue(0.1)
				.ValidationErrorMessage("[red]Must be between 0 and 1[/]")
				.Validate(n => n >= 0 && n <= 1));

		var options = new SearchOptions()
		{
			MinNgramSize = minNgramSize,
			MaxNgramSize = maxNgramSize,
			IncludeWordNgrams = includeWordNgrams,
			MinimumSimilarity = minimumSimilarity,
		};

		await Task.CompletedTask;
		return options;
	}

	private static async Task RunInteractiveSearch(FuzzySearchEngine searcher, int fileCount)
	{
		var panel = new Panel(
			$"[green]Loaded {fileCount} files[/]\n" +
			"• Type a search term to find matching files\n" +
			"• Enter a number to search by ID\n" +
			"• Type 'exit' to quit")
			.Header("[yellow]Interactive Fuzzy Search[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(panel);

		while (true)
		{
			var searchText = AnsiConsole.Prompt(
				new TextPrompt<string>("[blue]Search:[/]")
					.AllowEmpty());

			if (string.Equals(searchText, "exit", StringComparison.OrdinalIgnoreCase))
				break;

			if (string.IsNullOrWhiteSpace(searchText))
				continue;

			await AnsiConsole.Status()
				.Spinner(Spinner.Known.Dots)
				.StartAsync("Searching...", async ctx =>
				{
					var results = await searcher.SearchAsync(searchText);

					if (!results.Any())
					{
						AnsiConsole.MarkupLine("[red]No results found.[/]");
					}
					else
					{
						var table = new Table()
							.Border(TableBorder.Rounded)
							.Title($"[yellow]Results for '{searchText}'[/]")
							.AddColumn("Score", c => c.Width(8))
							.AddColumn("Filename")
							.AddColumn("Match", c => c.Width(15));

						foreach (var result in results.Take(10))
						{
							var matchQuality = result.Score switch
							{
								1.0 => "[green]Perfect[/]",
								>= 0.8 => "[green]Excellent[/]",
								>= 0.6 => "[yellow]Good[/]",
								>= 0.4 => "[orange1]Fair[/]",
								_ => "[red]Poor[/]"
							};

							table.AddRow(
								$"{result.Score:F3}",
								result.Text,
								matchQuality);
						}

						AnsiConsole.Write(table);

						if (results.Count() > 10)
						{
							AnsiConsole.MarkupLine($"[grey]...and {results.Count() - 10} more results[/]");
						}
					}
				});
		}

		AnsiConsole.MarkupLine("\n[yellow]Thanks for using Fuzzy Search![/]");
	}
}