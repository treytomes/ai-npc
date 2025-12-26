using LLM.NLP.REPL.Renderers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Regression tests to ensure punctuation handling remains stable
/// during NLP parsing.
/// </summary>
public sealed class NlpParser_PunctuationTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParser_PunctuationTests()
	{
		var services = new ServiceCollection();

		services.AddNlpRuntime(o =>
		{
			o.DataPath = "catalyst-data";
			o.Language = Language.English;
		});

		_provider = services.BuildServiceProvider();

		_runtime = _provider.GetRequiredService<INlpRuntime>();
		_parser = _provider.GetRequiredService<INlpParser>();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]NLP Parser — Punctuation Handling[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		_provider.Dispose();

		AnsiConsole.Write(
			new Rule("[dim]End Punctuation Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Parser_Removes_TerminalPunctuation()
	{
		const string input = "Open the door.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		ParsedInputSnapshotRenderer.Render(input, parsed);

		Assert.Equal(
			["open", "the", "door"],
			parsed.Tokens);

		Assert.Equal("open the door", parsed.NormalizedText);
	}

	[Fact]
	public void Parser_Preserves_InternalWordPunctuation()
	{
		const string input = "Use the x-ray machine.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		ParsedInputSnapshotRenderer.Render(input, parsed);

		Assert.Contains("x-ray", parsed.Tokens);
	}
}
