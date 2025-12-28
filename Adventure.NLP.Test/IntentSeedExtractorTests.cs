using Adventure.NLP.Services;
using Adventure.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.Test;

/// <summary>
/// Tests for extracting intent seeds from parsed input.
/// Uses snapshot-style Spectre.Console output for visibility.
/// </summary>
public sealed class IntentSeedExtractorTests
{
	private readonly IServiceProvider _provider;
	private readonly IIntentSeedExtractor _extractor;

	public IntentSeedExtractorTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<IIntentSeedExtractor>();
	}

	[Fact]
	public void Extractor_Finds_Verb_And_DirectObject()
	{
		var parsed = new ParsedInputBuilder()
			.Token("open", "open", NlpPartOfSpeech.Verb)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

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
			.Token("look", "look", NlpPartOfSpeech.Verb)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Equal("look", seed.Verb);
		Assert.Null(seed.DirectObject);
		Assert.Empty(seed.Prepositions);
	}

	[Fact]
	public void Extractor_Handles_No_Verb()
	{
		var parsed = new ParsedInputBuilder()
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Null(seed.Verb);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
	}

	[Fact]
	public void Extractor_Uses_Pos_Tagged_Verb()
	{
		var parsed = new ParsedInputBuilder()
			.Token("opened", "open", NlpPartOfSpeech.Verb)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Equal("open", seed.Verb);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
	}

	[Fact]
	public void Snapshot_Take_Key_From_Chest_In_Room()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", pos: NlpPartOfSpeech.Verb)
			.Token("key", pos: NlpPartOfSpeech.Noun)
			.Token("from", pos: NlpPartOfSpeech.Adposition)
			.Token("chest", pos: NlpPartOfSpeech.Noun)
			.Token("in", pos: NlpPartOfSpeech.Adposition)
			.Token("room", pos: NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

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
			.Token("take", pos: NlpPartOfSpeech.Verb)
			.Token("rusty", pos: NlpPartOfSpeech.Adjective)
			.Token("old", pos: NlpPartOfSpeech.Adjective)
			.Token("knife", pos: NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

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
			.Token("take", pos: NlpPartOfSpeech.Verb)
			.Token("key", pos: NlpPartOfSpeech.Noun)
			.Token("from", pos: NlpPartOfSpeech.Adposition)
			.Token("wooden", pos: NlpPartOfSpeech.Adjective)
			.Token("chest", pos: NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

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
			.Token("take", pos: NlpPartOfSpeech.Verb)
			.Token("the", pos: NlpPartOfSpeech.Determiner)
			.Token("old", pos: NlpPartOfSpeech.Adjective)
			.Token("key", pos: NlpPartOfSpeech.Noun)
			.Token("out", pos: NlpPartOfSpeech.Adjective)
			.Token("of", pos: NlpPartOfSpeech.Adposition)
			.Token("the", pos: NlpPartOfSpeech.Determiner)
			.Token("chest", pos: NlpPartOfSpeech.Noun)
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
			.Token("Show", "show", NlpPartOfSpeech.Verb)
			.Token("me", "me", NlpPartOfSpeech.Pronoun)
			.Token("what", "what", NlpPartOfSpeech.Pronoun)
			.Token("you", "you", NlpPartOfSpeech.Pronoun)
			.Token("have", "have", NlpPartOfSpeech.Verb)
			.Token("for", "for", NlpPartOfSpeech.Adposition)
			.Token("sale", "sale", NlpPartOfSpeech.Noun)
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
			.Token("what", "what", NlpPartOfSpeech.Pronoun)
			.Token("do", "do", NlpPartOfSpeech.AuxiliaryVerb)
			.Token("you", "you", NlpPartOfSpeech.Pronoun)
			.Token("have", "have", NlpPartOfSpeech.Verb)
			.Token("for", "for", NlpPartOfSpeech.Adposition)
			.Token("sale", "sale", NlpPartOfSpeech.Noun)
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