using LLM.Intent.FuzzySearch;
using Spectre.Console;

namespace LLM.Intent.Demo;

/// <summary>
/// Provides an interactive test harness for the character vector-based fuzzy search functionality.
/// </summary>
internal static class CharacterVectorDemo
{
	private const int MaxSearchLength = 100;
	private const int MaxResultsToShow = 10;
	private static readonly string[] SafeFileExtensions = { ".exe", ".ini", ".log", ".xml", ".txt", ".config", ".manifest" };

	/// <summary>
	/// Runs an interactive test of the fuzzy search functionality using Windows system files.
	/// </summary>
	public static async Task<int> RunAsync()
	{
		AnsiConsole.Write(
			new FigletText("Fuzzy Search")
				.LeftJustified()
				.Color(Color.Cyan1));

		var useWindowsFiles = AnsiConsole.Confirm("Load files from Windows directory?", false);

		List<string> files;
		try
		{
			files = useWindowsFiles
				? await LoadWindowsFiles()
				: LoadSampleFiles();

			if (!files.Any())
			{
				AnsiConsole.MarkupLine("[red]No files loaded. Exiting.[/]");
				return 1;
			}

			AnsiConsole.MarkupLine($"[green]✓ Loaded {files.Count} files[/]");
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
			return 1;
		}

		var options = ConfigureSearchOptions();
		var searcher = new FuzzySearchEngine(files, options);

		await RunInteractiveSearch(searcher, files.Count);

		return 0;
	}

	private static async Task<List<string>> LoadWindowsFiles()
	{
		var files = new List<string>();
		var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

		if (!Directory.Exists(windowsPath))
		{
			throw new DirectoryNotFoundException($"Windows directory not found: {windowsPath}");
		}

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync("Loading safe files from Windows directory...", async ctx =>
			{
				await Task.Run(() =>
				{
					try
					{
						files = Directory.EnumerateFiles(windowsPath, "*.*", SearchOption.TopDirectoryOnly)
							.Where(IsUserSafeFile)
							.Select(Path.GetFileName)
							.Where(f => !string.IsNullOrEmpty(f))
							.Select(f => f!)
							.Take(1000) // Limit to prevent excessive loading
							.ToList();
					}
					catch (UnauthorizedAccessException)
					{
						// Use only files we can access
						files = Directory.EnumerateFiles(windowsPath, "*.exe", SearchOption.TopDirectoryOnly)
							.Union(Directory.EnumerateFiles(windowsPath, "*.ini", SearchOption.TopDirectoryOnly))
							.Where(f => CanAccessFile(f))
							.Select(Path.GetFileName)
							.Where(f => !string.IsNullOrEmpty(f))
							.Select(f => f!)
							.Take(100)
							.ToList();
					}
				});

				ctx.Status($"Loaded {files.Count} files");
			});

		return files;
	}

	private static bool IsUserSafeFile(string filePath)
	{
		try
		{
			var extension = Path.GetExtension(filePath).ToLowerInvariant();
			var fileName = Path.GetFileName(filePath).ToLowerInvariant();

			// Only include specific safe extensions
			if (!SafeFileExtensions.Contains(extension))
				return false;

			// Exclude known sensitive files
			var sensitiveFiles = new[] { "sam", "security", "software", "system", "default" };
			return !sensitiveFiles.Any(sf => fileName.Contains(sf));
		}
		catch
		{
			return false;
		}
	}

	private static bool CanAccessFile(string filePath)
	{
		try
		{
			using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static List<string> LoadSampleFiles()
	{
		return new List<string>
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
			"Education.xml",
			"calculator.exe",
			"mspaint.exe",
			"wordpad.exe",
			"cmd.exe",
			"powershell.exe",
			"taskmgr.exe"
		};
	}

	private static SearchOptions ConfigureSearchOptions()
	{
		var useDefaults = AnsiConsole.Confirm("Use default search options?", true);

		if (useDefaults)
		{
			return new SearchOptions();
		}

		var minNgramSize = AnsiConsole.Prompt(
			new TextPrompt<int>("Minimum n-gram size:")
				.DefaultValue(1)
				.ValidationErrorMessage("[red]Must be between 1 and 10[/]")
				.Validate(n => n >= 1 && n <= 10));

		var maxNgramSize = AnsiConsole.Prompt(
			new TextPrompt<int>("Maximum n-gram size:")
				.DefaultValue(3)
				.ValidationErrorMessage("[red]Must be between minimum and 10[/]")
				.Validate(n => n >= minNgramSize && n <= 10));

		var includeWordNgrams = AnsiConsole.Confirm("Include word n-grams?", true);

		var minimumSimilarity = AnsiConsole.Prompt(
			new TextPrompt<double>("Minimum similarity (0-1):")
				.DefaultValue(0.1)
				.ValidationErrorMessage("[red]Must be between 0 and 1[/]")
				.Validate(n => n >= 0 && n <= 1));

		return new SearchOptions
		{
			MinNgramSize = minNgramSize,
			MaxNgramSize = maxNgramSize,
			IncludeWordNgrams = includeWordNgrams,
			MinimumSimilarity = minimumSimilarity
		};
	}

	private static async Task RunInteractiveSearch(FuzzySearchEngine searcher, int fileCount)
	{
		var panel = new Panel(
			$"[green]Loaded {fileCount} files[/]\n" +
			"• Type a search term to find matching files\n" +
			"• Enter a number to search by ID\n" +
			"• Type 'exit' or press Ctrl+C to quit\n" +
			$"• Maximum search length: {MaxSearchLength} characters")
			.Header("[yellow]Interactive Fuzzy Search[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(panel);

		var searchCount = 0;
		const int maxSearches = 100;

		while (searchCount < maxSearches)
		{
			var searchText = AnsiConsole.Prompt(
				new TextPrompt<string>($"[blue]Search ({searchCount + 1}/{maxSearches}):[/]")
					.AllowEmpty());

			if (string.Equals(searchText, "exit", StringComparison.OrdinalIgnoreCase))
				break;

			if (string.IsNullOrWhiteSpace(searchText))
				continue;

			if (searchText.Length > MaxSearchLength)
			{
				AnsiConsole.MarkupLine($"[red]Search term too long. Maximum {MaxSearchLength} characters.[/]");
				continue;
			}

			searchCount++;
			await PerformSearch(searcher, searchText);
		}

		if (searchCount >= maxSearches)
		{
			AnsiConsole.MarkupLine($"\n[yellow]Maximum number of searches ({maxSearches}) reached.[/]");
		}

		AnsiConsole.MarkupLine("\n[yellow]Thanks for using Fuzzy Search![/]");
	}

	private static async Task PerformSearch(FuzzySearchEngine searcher, string searchText)
	{
		try
		{
			await AnsiConsole.Status()
				.Spinner(Spinner.Known.Dots)
				.StartAsync("Searching...", async ctx =>
				{
					var stopwatch = System.Diagnostics.Stopwatch.StartNew();
					var results = await searcher.SearchAsync(searchText);
					stopwatch.Stop();

					var resultsList = results.Take(MaxResultsToShow + 1).ToList();
					var totalCount = resultsList.Count;
					var hasMore = totalCount > MaxResultsToShow;

					if (!resultsList.Any())
					{
						AnsiConsole.MarkupLine("[red]No results found.[/]");
						return;
					}

					var table = new Table()
						.Border(TableBorder.Rounded)
						.Title($"[yellow]Results for '{Markup.Escape(searchText)}' ({stopwatch.ElapsedMilliseconds}ms)[/]")
						.AddColumn("Score", c => c.Width(8))
						.AddColumn("Filename")
						.AddColumn("Match", c => c.Width(15));

					foreach (var result in resultsList.Take(MaxResultsToShow))
					{
						var matchQuality = GetMatchQuality(result.Score);

						table.AddRow(
							$"{result.Score:F3}",
							Markup.Escape(result.Text),
							matchQuality);
					}

					AnsiConsole.Write(table);

					if (hasMore)
					{
						AnsiConsole.MarkupLine($"[grey]...more results available (showing top {MaxResultsToShow})[/]");
					}

					// Show statistics
					var stats = new Rule($"[grey]Found {(hasMore ? $"{MaxResultsToShow}+" : totalCount.ToString())} results in {stopwatch.ElapsedMilliseconds}ms[/]")
						.RuleStyle("grey")
						.LeftJustified();
					AnsiConsole.Write(stats);
				});
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Search error: {Markup.Escape(ex.Message)}[/]");
		}
	}

	private static string GetMatchQuality(double score) => score switch
	{
		1.0 => "[green]Perfect[/]",
		>= 0.8 => "[green]Excellent[/]",
		>= 0.6 => "[yellow]Good[/]",
		>= 0.4 => "[orange1]Fair[/]",
		_ => "[red]Poor[/]"
	};
}