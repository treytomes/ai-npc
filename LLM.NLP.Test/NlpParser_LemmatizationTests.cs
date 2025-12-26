using LLM.NLP.REPL.Renderers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Integration tests for lemmatization behavior.
/// Verifies that verbs and nouns are normalized correctly.
/// </summary>
public sealed class NlpParser_LemmatizationTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParser_LemmatizationTests()
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
			new Rule("[bold green]NLP Parser — Lemmatization[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		_provider.Dispose();

		AnsiConsole.Write(
			new Rule("[dim]End Lemmatization Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Parser_Lemmatizes_Verbs_And_Nouns()
	{
		const string input = "The doors were opened.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		ParsedInputSnapshotRenderer.Render(input, parsed);

		Assert.Contains("door", parsed.Lemmas);
		Assert.Contains("open", parsed.Lemmas);
	}
}
