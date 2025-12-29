using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Adventure.NLP.REPL.SystemIntent;
using LLM.NPL.REPL;
using Adventure.NLP.REPL.SystemIntent.Commands;
using Adventure.NLP.Services;
using Adventure.NLP.SystemIntent;

namespace Adventure.NLP.REPL;

internal static class Program
{
	#region Fields

	private static bool _showPipeline = true;
	private static bool _showRawDocument = false;
	private static bool _showParseTree = true;
	private static bool _useJsonRenderers = false;
	private static bool _useCompactJson = false;

	private static INlpRuntime _runtime = null!;
	private static INlpParser _parser = null!;
	private static IIntentSeedExtractor _intentExtractor = null!;
	private static ISystemIntentEvaluator _systemIntentEvaluator = null!;

	#endregion

	#region Methods

	static void Main()
	{
		Bootstrap();

		RenderHeader();

		while (true)
		{
			var input = ReadInput();
			if (string.IsNullOrWhiteSpace(input))
				continue;

			ProcessInput(input);
		}
	}

	// -----------------------------
	// Bootstrap
	// -----------------------------

	private static void Bootstrap()
	{
		var services = new ServiceCollection();

		services.AddNlpRuntime();
		services.AddREPL();

		using var provider = services.BuildServiceProvider();

		_runtime = provider.GetRequiredService<INlpRuntime>();
		_parser = provider.GetRequiredService<INlpParser>();
		_intentExtractor = provider.GetRequiredService<IIntentSeedExtractor>();
		_systemIntentEvaluator = provider.GetRequiredService<ISystemIntentEvaluator>();

		_systemIntentEvaluator.AddCommands(new List<ISystemCommand>()
		{
			new ExitCommand(args => {
				AnsiConsole.MarkupLine("[grey]Goodbye.[/]");
				Environment.Exit(0);
			}),
			new HelpCommand(args => RenderHelp()),
			new ClearCommand(args => RenderHeader()),
			new TogglePipelineCommand(args =>
			{
				_showPipeline = args.GetValueOrDefault("enabled", !_showPipeline, false);
				AnsiConsole.MarkupLine($"[grey]Pipeline rendering: {(_showPipeline ? "[green]ON[/]" : "[red]OFF[/]")}[/]");
			}),
			new ToggleRawCommand(args =>
			{
				_showRawDocument = args.GetValueOrDefault("enabled", !_showRawDocument, false);
				AnsiConsole.MarkupLine($"[grey]Raw document output: {(_showRawDocument ? "[green]ON[/]" : "[red]OFF[/]")}[/]");
			}),
			new ToggleTreeCommand(args =>
			{
				_showParseTree = args.GetValueOrDefault("enabled", !_showParseTree, false);
				AnsiConsole.MarkupLine($"[grey]Parse tree output: {(_showParseTree ? "[green]ON[/]" : "[red]OFF[/]")}[/]");
			}),
			new ToggleJsonCommand(args =>
			{
				_useJsonRenderers = args.GetValueOrDefault("enabled", !_useJsonRenderers, false);
				_useCompactJson = args.GetValueOrDefault("compact", !_useCompactJson, false);
				AnsiConsole.MarkupLine($"[grey]JSON output: {(_useJsonRenderers ? "[green]ON[/]" : "[red]OFF[/]")}, compact: {(_useCompactJson ? "[green]ON[/]" : "[red]OFF[/]")}[/]");
			})
		});
	}

	// -----------------------------
	// Rendering
	// -----------------------------

	private static void RenderHeader()
	{
		AnsiConsole.Clear();

		AnsiConsole.Write(
			new FigletText("Adventure.NLP")
				.Color(Color.Cyan));

		AnsiConsole.MarkupLine(
			"[grey]Type natural language commands. Use :help for options.[/]");
		AnsiConsole.WriteLine();
	}

	private static string ReadInput()
	{
		return AnsiConsole.Prompt(
			new TextPrompt<string>("[bold green]>[/] ")
				.AllowEmpty());
	}

	// -----------------------------
	// Command Handling
	// -----------------------------

	private static void RenderHelp()
	{
		var table = new Table()
			.AddColumn("Command")
			.AddColumn("Description");

		table.AddRow(":help", "Show this help");
		table.AddRow(":exit", "Exit REPL");
		table.AddRow(":clear", "Clear screen");
		table.AddRow(":pipeline", "Toggle pipeline snapshots");
		table.AddRow(":raw", "Toggle raw document output");
		table.AddRow(":tree", "Toggle parse tree output");
		table.AddRow(":json", "Toggle JSON renderers");

		AnsiConsole.Write(table);
	}

	// -----------------------------
	// NLP Processing
	// -----------------------------

	private static void ProcessInput(string input)
	{
		AnsiConsole.WriteLine();

		try
		{
			var isSystemCommand = input.TrimStart().First() == ':';

			var document =
				_runtime.Process(input)
				?? throw new NullReferenceException("Processed document is null.");

			if (_showRawDocument)
			{
				AnsiConsole.MarkupLine("[bold]Raw Document[/]");
				if (_useJsonRenderers)
				{
					AnsiConsole.Write(document.ToJsonRenderable(_useCompactJson));
					AnsiConsole.WriteLine();
				}
				else
				{
					AnsiConsole.WriteLine(document.ToString() ?? "empty");
				}
				AnsiConsole.WriteLine();
			}

			var parsed = _parser.Parse(document);

			if (isSystemCommand)
			{
				_systemIntentEvaluator.TryEvaluate(parsed);
				return;
			}

			if (_showPipeline)
			{
				if (_useJsonRenderers)
				{
					AnsiConsole.Write(parsed.ToJsonRenderable(_useCompactJson));
					AnsiConsole.WriteLine();
				}
				else
				{
					AnsiConsole.Write(parsed.ToSnapshotRenderable(input));
				}
			}

			var intentSeed = _intentExtractor.Extract(parsed);

			if (intentSeed != null)
			{
				if (_useJsonRenderers)
				{
					AnsiConsole.Write(intentSeed.ToJsonRenderable(_useCompactJson));
					AnsiConsole.WriteLine();
				}
				else
				{
					AnsiConsole.Write(intentSeed.ToAnalysisRenderable(input, parsed));
				}

				if (_showParseTree)
				{
					if (_useJsonRenderers)
					{
						AnsiConsole.Write(new
						{
							input,
							parsed,
							intentSeed,
						}.ToJsonRenderable(_useCompactJson));
						AnsiConsole.WriteLine();
					}
					else
					{
						AnsiConsole.Write(parsed.ToParseTreeRenderable(input, intentSeed));
					}
				}
			}
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
		}

		AnsiConsole.WriteLine();
	}

	#endregion
}
