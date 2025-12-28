using Adventure.Intent.FuzzySearch;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Adventure.States;

internal class FuzzySearchTestState : AppState
{
	#region Fields

	private readonly ILogger<FuzzySearchTestState> _logger;
	private readonly List<string> _failedAssertions = new();

	#endregion

	#region Constructors

	public FuzzySearchTestState(IStateManager states, ILogger<FuzzySearchTestState> logger)
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
		AnsiConsole.Write(
			new Rule("[yellow]Fuzzy Search Tests[/]")
				.RuleStyle("grey")
				.LeftJustified());

		var testRunner = this;
		var tests = testRunner.DiscoverTests();

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Test")
			.AddColumn("Result")
			.AddColumn("Details");

		foreach (var (name, description, testMethod) in tests)
		{
			var result = await testRunner.RunTestAsync(name, testMethod);

			table.AddRow(
				$"[bold]{name}[/]\n[grey]{description}[/]",
				result.Success ? "[green]✓ PASS[/]" : "[red]✗ FAIL[/]",
				$"{result.Details} [grey]({result.Duration}ms)[/]");
		}

		AnsiConsole.Write(table);

		AnsiConsole.Prompt(
			new TextPrompt<string>($"[blue]Press enter to continue.[/]")
				.AllowEmpty());

		await LeaveAsync();
	}

	private List<(string Name, string Description, Func<Task> Test)> DiscoverTests()
	{
		var tests = new List<(string, string, Func<Task>)>();
		var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		foreach (var method in methods)
		{
			var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();
			if (descriptionAttr != null && method.ReturnType == typeof(Task) && method.GetParameters().Length == 0)
			{
				var testDelegate = (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>), this, method);
				tests.Add((method.Name, descriptionAttr.Description, testDelegate));
			}
		}

		return tests;
	}

	private async Task<TestResult> RunTestAsync(string name, Func<Task> test)
	{
		_failedAssertions.Clear();
		var stopwatch = Stopwatch.StartNew();

		try
		{
			await test();
			stopwatch.Stop();

			return new TestResult
			{
				Name = name,
				Success = _failedAssertions.Count == 0,
				Details = _failedAssertions.Count == 0 ? "All assertions passed" : string.Join("; ", _failedAssertions),
				Duration = stopwatch.ElapsedMilliseconds
			};
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			return new TestResult
			{
				Name = name,
				Success = false,
				Details = $"Exception: {ex.Message}",
				Duration = stopwatch.ElapsedMilliseconds
			};
		}
	}

	[Description("Tests that exact matches return with the highest score")]
	private async Task TestExactMatch()
	{
		var items = new[] { "test", "testing", "tested" };
		var engine = new FuzzySearchEngine(items);

		var results = await engine.SearchAsync("test");
		var topResult = results.FirstOrDefault();

		Assert(() => topResult != null, "Should return results");
		if (topResult == null) return;

		Assert(() => topResult.Text == "test", "Exact match should be first");
		Assert(() => topResult.Score > 0.99, "Exact match should have near-perfect score");
	}

	[Description("Tests fuzzy matching with typos and similar strings")]
	private async Task TestFuzzyMatch()
	{
		var items = new[] { "Microsoft", "Minecraft", "Microwave" };
		var engine = new FuzzySearchEngine(items);

		var results = await engine.SearchAsync("Microsft"); // Missing 'o'
		var topResult = results.FirstOrDefault();

		Assert(() => topResult != null, "Should return results for fuzzy match");
		if (topResult == null) return;

		Assert(() => topResult.Text == "Microsoft", "Should match despite typo");
		Assert(() => topResult.Score > 0.7, "Should have high similarity score");
	}

	[Description("Tests searching by numeric ID")]
	private async Task TestIdSearch()
	{
		var items = new[] { "First", "Second", "Third" };
		var engine = new FuzzySearchEngine(items);

		var results = await engine.SearchAsync("1");
		var result = results.FirstOrDefault();

		Assert(() => result != null, "Should find item by ID");
		if (result == null) return;

		Assert(() => result.Text == "Second", "Should match correct item");
		Assert(() => result.Score == 1.0, "ID match should have perfect score");
	}

	[Description("Tests handling of empty, null, and whitespace queries")]
	private async Task TestEmptyQuery()
	{
		var items = new[] { "Test" };
		var engine = new FuzzySearchEngine(items);

		var results1 = await engine.SearchAsync("");
		var results2 = await engine.SearchAsync(null);
		var results3 = await engine.SearchAsync("   ");

		Assert(() => !results1.Any(), "Empty query should return no results");
		Assert(() => !results2.Any(), "Null query should return no results");
		Assert(() => !results3.Any(), "Whitespace query should return no results");
	}

	private void Assert(Func<bool> condition, string message)
	{
		try
		{
			if (!condition())
			{
				_failedAssertions.Add(message);
			}
		}
		catch
		{
			_failedAssertions.Add(message);
		}
	}

	#endregion

	private class TestResult
	{
		public string Name { get; init; } = string.Empty;
		public bool Success { get; init; }
		public string Details { get; init; } = string.Empty;
		public long Duration { get; init; }
	}
}