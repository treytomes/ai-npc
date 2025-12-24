using Catalyst;
using LLM.NLP.REPL;
using LLM.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Integration-style tests for imperative command parsing.
/// Validates runtime + parser behavior together.
/// </summary>
public sealed class NlpParser_ImperativeTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParser_ImperativeTests()
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
			new Rule("[bold green]NLP Parser — Imperative Commands[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		_provider.Dispose();

		AnsiConsole.Write(
			new Rule("[dim]End Imperative Parser Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Theory]
	[InlineData("open door")]
	[InlineData("take the sword")]
	[InlineData("look around")]
	public void Parser_Handles_Imperative_Commands(string input)
	{
		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		ParsedInputSnapshotRenderer.Render(input, parsed);

		Assert.NotEmpty(parsed.Lemmas);
	}

	[Fact]
	public void Parser_Preserves_Stopwords_In_Lemmas()
	{
		var input = "open the door";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		ParsedInputSnapshotRenderer.Render(input, parsed);

		Assert.Contains("the", parsed.Lemmas);
	}
}
