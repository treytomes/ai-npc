using LLM.NLP.REPL.Renderers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Verifies that the NLP parser converts processed documents into
/// normalized <see cref="ParsedInput"/> instances.
/// </summary>
public sealed class NlpParserTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParserTests()
	{
		var services = new ServiceCollection();

		services.AddNlpRuntime(options =>
		{
			options.DataPath = "catalyst-data";
			options.Language = Language.English;
		});

		_provider = services.BuildServiceProvider();

		_runtime = _provider.GetRequiredService<INlpRuntime>();
		_parser = _provider.GetRequiredService<INlpParser>();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]NLP Parser — Normalization[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		_provider.Dispose();

		AnsiConsole.Write(
			new Rule("[dim]End Parser Normalization Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Parser_Produces_NormalizedParsedInput()
	{
		const string input = "Open the door.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		ParsedInputSnapshotRenderer.Render(input, parsed);

		Assert.Equal("Open the door.", parsed.RawText);
		Assert.Equal("open the door", parsed.NormalizedText);

		Assert.Equal(
			new[] { "open", "the", "door" },
			parsed.Tokens);
	}
}
