using Catalyst;
using LLM.NLP.REPL.Renderers;
using LLM.NLP.Services;
using LLM.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Tests for extracting intent seeds from parsed input.
/// Uses snapshot-style Spectre.Console output for visibility.
/// </summary>
public sealed class IntentSeedExtractorTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly IIntentSeedExtractor _extractor;

	public IntentSeedExtractorTests()
	{
		var services = new ServiceCollection();

		services.AddNlpRuntime(o =>
		{
			o.DataPath = "catalyst-data";
			o.Language = Language.English;
		});

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<IIntentSeedExtractor>();

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

		Assert.NotNull(seed.DirectObject);

		var door = seed.DirectObject!;
		Assert.Equal("door", door.Head);
		Assert.Equal("the door", door.Text);
		Assert.Equal(["the"], door.Modifiers);

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
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
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
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
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

		IntentSeedSnapshotRenderer.Render(parsed.RawText, parsed, seed);

		Assert.Equal("take", seed.Verb);

		Assert.NotNull(seed.DirectObject);
		Assert.Equal("key", seed.DirectObject!.Head);

		Assert.NotNull(seed.DirectObject);

		var key = seed.DirectObject!;
		Assert.Equal("key", key.Head);

		Assert.True(key.Complements.ContainsKey("from"));

		var chest = key.Complements["from"];
		Assert.Equal("chest", chest.Head);

		Assert.True(chest.Complements.ContainsKey("in"));
		Assert.Equal("room", chest.Complements["in"].Head);
	}

	[Fact]
	public void Extractor_Builds_Modified_NounPhrase()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", pos: PartOfSpeech.VERB)
			.Token("rusty", pos: PartOfSpeech.ADJ)
			.Token("old", pos: PartOfSpeech.ADJ)
			.Token("knife", pos: PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render(parsed.RawText, parsed, seed);

		var np = seed.DirectObject!;
		Assert.Equal("knife", np.Head);
		Assert.Equal(
			new[] { "rusty", "old" },
			np.Modifiers);
		Assert.Equal("rusty old knife", np.Text);
	}

	[Fact]
	public void Extractor_Builds_Nested_NounPhrase_Complements()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", pos: PartOfSpeech.VERB)
			.Token("key", pos: PartOfSpeech.NOUN)
			.Token("from", pos: PartOfSpeech.ADP)
			.Token("wooden", pos: PartOfSpeech.ADJ)
			.Token("chest", pos: PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render(parsed.RawText, parsed, seed);

		Assert.Equal("take", seed.Verb);
		Assert.NotNull(seed.DirectObject);

		var key = seed.DirectObject!;
		Assert.Equal("key", key.Head);

		Assert.True(key.Complements.ContainsKey("from"));

		var chest = key.Complements["from"];
		Assert.Equal("chest", chest.Head);
		Assert.Equal(["wooden"], chest.Modifiers);
		Assert.Equal("wooden chest", chest.Text);
	}

	[Fact]
	public void Extracts_Out_Of_Phrasal_Prep()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", pos: PartOfSpeech.VERB)
			.Token("the", pos: PartOfSpeech.DET)
			.Token("old", pos: PartOfSpeech.ADJ)
			.Token("key", pos: PartOfSpeech.NOUN)
			.Token("out", pos: PartOfSpeech.ADJ)
			.Token("of", pos: PartOfSpeech.ADP)
			.Token("the", pos: PartOfSpeech.DET)
			.Token("chest", pos: PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Equal("key", seed.DirectObject!.Head);
		Assert.Equal(["the", "old"], seed.DirectObject.Modifiers);

		Assert.True(seed.DirectObject.Complements.ContainsKey("out of"));
		Assert.Equal(
			"chest",
			seed.DirectObject.Complements["out of"].Head);
	}

	[Fact]
	public void Extracts_Show_Me_What_You_Have_For_Sale()
	{
		var parsed = new ParsedInputBuilder()
			.Token("Show", "show", PartOfSpeech.VERB)
			.Token("me", "me", PartOfSpeech.PRON)
			.Token("what", "what", PartOfSpeech.PRON)
			.Token("you", "you", PartOfSpeech.PRON)
			.Token("have", "have", PartOfSpeech.VERB)
			.Token("for", "for", PartOfSpeech.ADP)
			.Token("sale", "sale", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		// Should extract "show" as main verb
		Assert.Equal("show", seed.Verb);

		// Should extract "me" as indirect object
		Assert.NotNull(seed.IndirectObject);
		Assert.Equal("me", seed.IndirectObject!.Head);
		Assert.Equal("me", seed.IndirectObject.Text);

		// Should extract "what you have" as direct object (relative clause)
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("what", seed.DirectObject!.Head);
		Assert.Equal("what you have", seed.DirectObject.Text);

		// Should extract "for sale" as prepositional phrase
		Assert.Single(seed.Prepositions);
		Assert.True(seed.Prepositions.ContainsKey("for"));
		Assert.Equal("sale", seed.Prepositions["for"].Head);
		Assert.Equal("sale", seed.Prepositions["for"].Text);
	}

	[Fact]
	public void Extracts_What_Do_You_Have_For_Sale()
	{
		var parsed = new ParsedInputBuilder()
			.Token("what", "what", PartOfSpeech.PRON)
			.Token("do", "do", PartOfSpeech.AUX)
			.Token("you", "you", PartOfSpeech.PRON)
			.Token("have", "have", PartOfSpeech.VERB)
			.Token("for", "for", PartOfSpeech.ADP)
			.Token("sale", "sale", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		// Should extract "have" as main verb (not auxiliary "do")
		Assert.Equal("have", seed.Verb);

		// Should extract "you" as subject
		Assert.NotNull(seed.Subject);
		Assert.Equal("you", seed.Subject!.Head);
		Assert.Equal("you", seed.Subject.Text);

		// Should extract "what" as direct object
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("what", seed.DirectObject!.Head);
		Assert.Equal("what", seed.DirectObject.Text);

		// Should extract "for sale" as prepositional phrase
		Assert.Single(seed.Prepositions);
		Assert.True(seed.Prepositions.ContainsKey("for"));
		Assert.Equal("sale", seed.Prepositions["for"].Head);
		Assert.Equal("sale", seed.Prepositions["for"].Text);

		// No indirect object in this case
		Assert.Null(seed.IndirectObject);
	}
}