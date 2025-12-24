using Catalyst;
using LLM.NLP.REPL;
using LLM.NLP.Test.Helpers;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Tests for extracting intent seeds from parsed input.
/// Uses snapshot-style Spectre.Console output for visibility.
/// </summary>
public sealed class IntentSeedExtractorTests : IDisposable
{
	private readonly IntentSeedExtractor _extractor;

	public IntentSeedExtractorTests()
	{
		_extractor = new IntentSeedExtractor();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]IntentSeedExtractor — Core Cases[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		AnsiConsole.Write(
			new Rule("[dim]End Core Intent Seed Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Extractor_Finds_Verb_And_DirectObject()
	{
		var parsed = new ParsedInputBuilder()
			.Token("open", "open", PartOfSpeech.VERB)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("open the door", parsed, seed);

		Assert.Equal("open", seed.Verb);
		Assert.Equal("door", seed.DirectObject);
		Assert.Empty(seed.Prepositions);
	}

	[Fact]
	public void Extractor_Handles_No_Object()
	{
		var parsed = new ParsedInputBuilder()
			.Token("look", "look", PartOfSpeech.VERB)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("look", parsed, seed);

		Assert.Equal("look", seed.Verb);
		Assert.Null(seed.DirectObject);
		Assert.Empty(seed.Prepositions);
	}

	[Fact]
	public void Extractor_Handles_No_Verb()
	{
		var parsed = new ParsedInputBuilder()
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("the door", parsed, seed);

		Assert.Null(seed.Verb);
		Assert.Equal("door", seed.DirectObject);
		Assert.Empty(seed.Prepositions);
	}

	[Fact]
	public void Extractor_Uses_Pos_Tagged_Verb()
	{
		var parsed = new ParsedInputBuilder()
			.Token("opened", "open", PartOfSpeech.VERB)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("opened the door", parsed, seed);

		Assert.Equal("open", seed.Verb);
		Assert.Equal("door", seed.DirectObject);
		Assert.Empty(seed.Prepositions);
	}

	[Fact]
	public void Snapshot_Take_Key_From_Chest_In_Room()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", pos: PartOfSpeech.VERB)
			.Token("key", pos: PartOfSpeech.NOUN)
			.Token("from", pos: PartOfSpeech.ADP)
			.Token("chest", pos: PartOfSpeech.NOUN)
			.Token("in", pos: PartOfSpeech.ADP)
			.Token("room", pos: PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render(
			parsed.RawText,
			parsed,
			seed);

		Assert.Equal("take", seed.Verb);
		Assert.Equal("key", seed.DirectObject);
		Assert.Equal("chest", seed.Prepositions["from"]);
		Assert.Equal("room", seed.Prepositions["in"]);
	}
}
