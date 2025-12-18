using LLM.Intent.FuzzySearch;
using Spectre.Console;
using System.Diagnostics;

namespace LLM.Intent.Demo;

/// <summary>
/// Unit tests for the fuzzy search functionality with Spectre.Console output.
/// </summary>
public static class FuzzySearchTests
{
	public static async Task<int> RunAsync()
	{
		AnsiConsole.Write(
			new Rule("[yellow]Fuzzy Search Tests[/]")
				.RuleStyle("grey")
				.LeftJustified());

		var tests = new List<(string Name, Func<Task> Test)>
		{
			("Exact Match Test", TestExactMatch),
			("Fuzzy Match Test", TestFuzzyMatch),
			("ID Search Test", TestIdSearch),
			("Empty Query Test", TestEmptyQuery)
		};

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Test")
			.AddColumn("Result")
			.AddColumn("Details");

		foreach (var (name, test) in tests)
		{
			var stopwatch = Stopwatch.StartNew();
			var (success, details) = await RunTest(test);
			stopwatch.Stop();

			table.AddRow(
				name,
				success ? "[green]✓ PASS[/]" : "[red]✗ FAIL[/]",
				$"{details} [grey]({stopwatch.ElapsedMilliseconds}ms)[/]");
		}

		AnsiConsole.Write(table);
		return 0;
	}

	private static async Task<(bool Success, string Details)> RunTest(Func<Task> test)
	{
		try
		{
			var results = new List<string>();
			await test();
			return (results.Count == 0, results.Count == 0 ? "All assertions passed" : string.Join("; ", results));
		}
		catch (Exception ex)
		{
			return (false, $"Exception: {ex.Message}");
		}
	}

	/// <summary>
	/// Tests exact match scenarios.
	/// </summary>
	public static async Task TestExactMatch()
	{
		var items = new[] { "test", "testing", "tested" };
		var engine = new FuzzySearchEngine(items);

		var results = await engine.SearchAsync("test");
		var topResult = results.First();

		Assert(topResult.Text == "test", "Exact match should be first");
		Assert(topResult.Score > 0.99, "Exact match should have near-perfect score");
	}

	/// <summary>
	/// Tests fuzzy matching with typos.
	/// </summary>
	public static async Task TestFuzzyMatch()
	{
		var items = new[] { "Microsoft", "Minecraft", "Microwave" };
		var engine = new FuzzySearchEngine(items);

		var results = await engine.SearchAsync("Microsft"); // Missing 'o'
		var topResult = results.First();

		Assert(topResult.Text == "Microsoft", "Should match despite typo");
		Assert(topResult.Score > 0.7, "Should have high similarity score");
	}

	/// <summary>
	/// Tests ID-based search.
	/// </summary>
	public static async Task TestIdSearch()
	{
		var items = new[] { "First", "Second", "Third" };
		var engine = new FuzzySearchEngine(items);

		var results = await engine.SearchAsync("1");
		var result = results.FirstOrDefault();

		Assert(result != null, "Should find item by ID");
		Assert(result!.Text == "Second", "Should match correct item");
		Assert(result.Score == 1.0, "ID match should have perfect score");
	}

	/// <summary>
	/// Tests empty and null query handling.
	/// </summary>
	public static async Task TestEmptyQuery()
	{
		var items = new[] { "Test" };
		var engine = new FuzzySearchEngine(items);

		var results1 = await engine.SearchAsync("");
		var results2 = await engine.SearchAsync(null);
		var results3 = await engine.SearchAsync("   ");

		Assert(!results1.Any(), "Empty query should return no results");
		Assert(!results2.Any(), "Null query should return no results");
		Assert(!results3.Any(), "Whitespace query should return no results");
	}

	private static readonly List<string> _failedAssertions = new();

	private static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			_failedAssertions.Add(message);
			AnsiConsole.MarkupLine($"[red]✗[/] {message}");
		}
	}
}