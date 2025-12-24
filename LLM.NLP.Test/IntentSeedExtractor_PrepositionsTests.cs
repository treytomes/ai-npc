using Catalyst;
using LLM.NLP.REPL;
using LLM.NLP.Test.Helpers;
using Spectre.Console;

namespace LLM.NLP.Test;

public sealed class IntentSeedExtractor_PrepositionsTests : IDisposable
{
	private readonly IntentSeedExtractor _extractor;

	public IntentSeedExtractor_PrepositionsTests()
	{
		_extractor = new IntentSeedExtractor();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]IntentSeedExtractor — Prepositional Phrases[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		AnsiConsole.Write(
			new Rule("[dim]End Prepositional Phrase Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Extracts_From_Prepositional_Phrase()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", "take", PartOfSpeech.VERB)
			.Token("key", "key", PartOfSpeech.NOUN)
			.Token("from", "from", PartOfSpeech.ADP)
			.Token("chest", "chest", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("take key from chest", parsed, seed);

		Assert.Equal("take", seed.Verb);
		Assert.Equal("key", seed.DirectObject);
		Assert.Equal("chest", seed.Prepositions["from"]);
	}

	[Fact]
	public void Extracts_To_Prepositional_Phrase()
	{

		var parsed = new ParsedInputBuilder()
			.Token("put", "put", PartOfSpeech.VERB)
			.Token("book", "book", PartOfSpeech.NOUN)
			.Token("on", "on", PartOfSpeech.ADP)
			.Token("table", "table", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("put book on table", parsed, seed);

		Assert.Equal("put", seed.Verb);
		Assert.Equal("book", seed.DirectObject);
		Assert.Equal("table", seed.Prepositions["on"]);
	}
}
