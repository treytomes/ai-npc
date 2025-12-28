using Adventure.NLP.Services;
using Adventure.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.Test;

public sealed class IntentSeedExtractor_PrepositionsTests
{
	private readonly IServiceProvider _provider;
	private readonly IIntentSeedExtractor _extractor;

	public IntentSeedExtractor_PrepositionsTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<IIntentSeedExtractor>();
	}

	[Fact]
	public void Extracts_From_Prepositional_Phrase()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", "take", NlpPartOfSpeech.Verb)
			.Token("key", "key", NlpPartOfSpeech.Noun)
			.Token("from", "from", NlpPartOfSpeech.Adposition)
			.Token("chest", "chest", NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Equal("take", seed.Verb);

		Assert.NotNull(seed.DirectObject);
		Assert.Equal("key", seed.DirectObject!.Head);
		Assert.Equal("key from chest", seed.DirectObject.Text);

		Assert.Empty(seed.Prepositions);

		Assert.True(seed.DirectObject.Complements.ContainsKey("from"));
		var chest = seed.DirectObject.Complements["from"];

		Assert.Equal("chest", chest.Head);
		Assert.Equal("chest", chest.Text);
	}

	[Fact]
	public void Extracts_On_Prepositional_Phrase()
	{
		var parsed = new ParsedInputBuilder()
			.Token("put", "put", NlpPartOfSpeech.Verb)
			.Token("book", "book", NlpPartOfSpeech.Noun)
			.Token("on", "on", NlpPartOfSpeech.Adposition)
			.Token("table", "table", NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Equal("put", seed.Verb);

		Assert.NotNull(seed.DirectObject);
		Assert.Equal("book", seed.DirectObject!.Head);

		Assert.Empty(seed.Prepositions);

		Assert.True(seed.DirectObject.Complements.ContainsKey("on"));
		Assert.Equal("table", seed.DirectObject.Complements["on"].Head);
	}

	[Fact]
	public void Prepositional_Object_Preserves_Modifiers()
	{
		var parsed = new ParsedInputBuilder()
			.Token("put", pos: NlpPartOfSpeech.Verb)
			.Token("book", pos: NlpPartOfSpeech.Noun)
			.Token("on", pos: NlpPartOfSpeech.Adposition)
			.Token("wooden", pos: NlpPartOfSpeech.Adjective)
			.Token("table", pos: NlpPartOfSpeech.Noun)
			.Build();

		var seed = _extractor.Extract(parsed);

		var table = seed.DirectObject!
			.Complements["on"];

		Assert.Equal("table", table.Head);
		Assert.Equal(["wooden"], table.Modifiers);
		Assert.Equal("wooden table", table.Text);
	}

	[Fact]
	public void Multiple_Prepositional_Phrases_Are_Extracted()
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

		var key = seed.DirectObject!;
		Assert.True(key.Complements.ContainsKey("from"));

		var chest = key.Complements["from"];
		Assert.Equal("chest", chest.Head);

		Assert.True(chest.Complements.ContainsKey("in"));
		Assert.Equal("room", chest.Complements["in"].Head);
	}
}
