using Catalyst;
using LLM.NLP.REPL.Renderers;
using LLM.NLP.Services;
using LLM.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

public sealed class NounPhraseTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly INounPhraseExtractor _extractor;

	public NounPhraseTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<INounPhraseExtractor>();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]NounPhraseExtractor — Index-Based Tests[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		AnsiConsole.Write(
			new Rule("[dim]End Noun Phrase Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Extracts_Adjective_Modified_Noun()
	{
		var parsed = new ParsedInputBuilder()
			.Token("rusty", pos: PartOfSpeech.ADJ)
			.Token("old", pos: PartOfSpeech.ADJ)
			.Token("knife", pos: PartOfSpeech.NOUN)
			.Build();

		var phrases = ExtractAll(parsed);

		NounPhraseSnapshotRenderer.RenderAll(parsed.RawText, phrases);

		Assert.Single(phrases);
		Assert.Equal("knife", phrases[0].Head);
		Assert.Equal(new[] { "rusty", "old" }, phrases[0].Modifiers);
	}

	[Fact]
	public void Extracts_Of_Complement()
	{
		var parsed = new ParsedInputBuilder()
			.Token("loaf", pos: PartOfSpeech.NOUN)
			.Token("of", pos: PartOfSpeech.ADP)
			.Token("bread", pos: PartOfSpeech.NOUN)
			.Build();

		var phrases = ExtractAll(parsed);

		NounPhraseSnapshotRenderer.RenderAll(parsed.RawText, phrases);

		var np = phrases.Single();
		Assert.Equal("loaf", np.Head);
		Assert.Equal("bread", np.Complements["of"].Head);
	}

	[Fact]
	public void Extracts_compound_nouns()
	{
		var parsed = new ParsedInputBuilder()
			.Token("toggle", pos: PartOfSpeech.NOUN)
			.Token("document", pos: PartOfSpeech.NOUN)
			.Build();

		var phrases = ExtractAll(parsed);

		NounPhraseSnapshotRenderer.RenderAll(parsed.RawText, phrases);

		var np = phrases.Single();
		Assert.Equal("document", np.Head);
		Assert.True(np.Modifiers?.Contains("toggle"));
	}

	/* ---------------- helpers ---------------- */

	private List<NounPhrase> ExtractAll(ParsedInput parsed)
	{
		var phrases = new List<NounPhrase>();
		var tokens = parsed.ParsedTokens;
		int i = 0;

		while (i < tokens.Count)
		{
			var start = i;
			var phrase = _extractor.TryExtract(tokens, ref i);

			if (phrase != null)
				phrases.Add(phrase);
			else
				i = Math.Max(i, start + 1);
		}

		return phrases;
	}
}
