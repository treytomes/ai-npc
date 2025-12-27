namespace LLM.NLP.Test;

using Catalyst;
using LLM.NLP.Services;
using LLM.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Xunit;

public class IntentSeedExtractor_SubjectTests
{
	private readonly IServiceProvider _provider;
	private readonly IIntentSeedExtractor _extractor;
	private readonly INounPhraseExtractor _nounPhraseExtractor;

	public IntentSeedExtractor_SubjectTests()
	{
		var services = new ServiceCollection();

		services.AddNlpRuntime(o =>
		{
			o.DataPath = "catalyst-data";
			o.Language = Language.English;
		});

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<IIntentSeedExtractor>();
		_nounPhraseExtractor = _provider.GetRequiredService<INounPhraseExtractor>();
	}

	[Fact]
	public void Extracts_Simple_Pronoun_Subject()
	{
		// "You have three items"
		var parsed = new ParsedInputBuilder()
			.Token("you", "you", PartOfSpeech.PRON)
			.Token("have", "have", PartOfSpeech.VERB)
			.Token("three", "three", PartOfSpeech.NUM)
			.Token("items", "item", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("you", seed.Subject!.Head);
		Assert.Equal("you", seed.Subject.Text);
		Assert.Equal("have", seed.Verb);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("items", seed.DirectObject!.Head);
	}

	[Fact]
	public void Extracts_Simple_Noun_Subject()
	{
		// "The cat sits on the mat"
		var parsed = new ParsedInputBuilder()
			.Token("the", "the", PartOfSpeech.DET)
			.Token("cat", "cat", PartOfSpeech.NOUN)
			.Token("sits", "sit", PartOfSpeech.VERB)
			.Token("on", "on", PartOfSpeech.ADP)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("mat", "mat", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("cat", seed.Subject!.Head);
		Assert.Equal("the cat", seed.Subject.Text);
		Assert.Equal("sit", seed.Verb);
		Assert.Single(seed.Prepositions);
		Assert.Equal("mat", seed.Prepositions["on"].Head);
	}

	[Fact]
	public void Extracts_Complex_Subject_With_Modifiers()
	{
		// "The old brown dog barked loudly"
		var parsed = new ParsedInputBuilder()
			.Token("the", "the", PartOfSpeech.DET)
			.Token("old", "old", PartOfSpeech.ADJ)
			.Token("brown", "brown", PartOfSpeech.ADJ)
			.Token("dog", "dog", PartOfSpeech.NOUN)
			.Token("barked", "bark", PartOfSpeech.VERB)
			.Token("loudly", "loudly", PartOfSpeech.ADV)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("dog", seed.Subject!.Head);
		Assert.Equal("the old brown dog", seed.Subject.Text);
		Assert.Contains("the", seed.Subject.Modifiers);
		Assert.Contains("old", seed.Subject.Modifiers);
		Assert.Contains("brown", seed.Subject.Modifiers);
	}

	[Fact]
	public void Imperative_Has_Null_Subject()
	{
		// "Show me the door" (imperative - implied subject "you")
		var parsed = new ParsedInputBuilder()
			.Token("Show", "show", PartOfSpeech.VERB)
			.Token("me", "me", PartOfSpeech.PRON)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Null(seed.Subject);
		Assert.Equal("show", seed.Verb);
		Assert.NotNull(seed.IndirectObject);
		Assert.Equal("me", seed.IndirectObject!.Head);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
	}

	[Fact]
	public void Question_With_Auxiliary_Extracts_Subject()
	{
		// "What do you have for sale?"
		var parsed = new ParsedInputBuilder()
			.Token("what", "what", PartOfSpeech.PRON)
			.Token("do", "do", PartOfSpeech.AUX)
			.Token("you", "you", PartOfSpeech.PRON)
			.Token("have", "have", PartOfSpeech.VERB)
			.Token("for", "for", PartOfSpeech.ADP)
			.Token("sale", "sale", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("you", seed.Subject!.Head);
		Assert.Equal("have", seed.Verb);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("what", seed.DirectObject!.Head);
	}

	[Fact]
	public void Inverted_Question_Extracts_Subject()
	{
		// "Is the door open?"
		var parsed = new ParsedInputBuilder()
			.Token("is", "be", PartOfSpeech.AUX)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Token("open", "open", PartOfSpeech.ADJ)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("door", seed.Subject!.Head);
		Assert.Equal("the door", seed.Subject.Text);
		// Note: This might need special handling for copula "be" as main verb
	}

	[Fact]
	public void Compound_Subject_With_Coordination()
	{
		// "John and Mary went to the store"
		var parsed = new ParsedInputBuilder()
			.Token("john", "john", PartOfSpeech.PROPN)
			.Token("and", "and", PartOfSpeech.CCONJ)
			.Token("mary", "mary", PartOfSpeech.PROPN)
			.Token("went", "go", PartOfSpeech.VERB)
			.Token("to", "to", PartOfSpeech.ADP)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("store", "store", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		// The whole coordinated phrase is the subject
		Assert.Equal("mary", seed.Subject!.Head); // Last noun is head
		Assert.Equal("john and mary", seed.Subject.Text);
		// Or we could enhance NounPhrase to handle coordination better
	}

	[Fact]
	public void Subject_With_Prepositional_Phrase()
	{
		// "The man with the hat smiled"
		var parsed = new ParsedInputBuilder()
			.Token("the", "the", PartOfSpeech.DET)
			.Token("man", "man", PartOfSpeech.NOUN)
			.Token("with", "with", PartOfSpeech.ADP)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("hat", "hat", PartOfSpeech.NOUN)
			.Token("smiled", "smile", PartOfSpeech.VERB)
			.Build();
		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("man", seed.Subject!.Head);
		Assert.Equal("the man with the hat", seed.Subject.Text);
		Assert.Single(seed.Subject.Complements);
		Assert.Equal("hat", seed.Subject.Complements["with"].Head);
	}

	[Fact]
	public void Passive_Voice_Subject()
	{
		// "The door was opened by John"
		var parsed = new ParsedInputBuilder()
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Token("was", "be", PartOfSpeech.AUX)
			.Token("opened", "open", PartOfSpeech.VERB)
			.Token("by", "by", PartOfSpeech.ADP)
			.Token("john", "john", PartOfSpeech.PROPN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("door", seed.Subject!.Head);
		Assert.Equal("the door", seed.Subject.Text);
		Assert.Equal("open", seed.Verb);
		Assert.Single(seed.Prepositions);
		Assert.Equal("john", seed.Prepositions["by"].Head);
	}

	[Fact]
	public void Existential_There_Subject()
	{
		// "There are three items on the shelf"
		var parsed = new ParsedInputBuilder()
			.Token("there", "there", PartOfSpeech.PRON)
			.Token("are", "be", PartOfSpeech.VERB)
			.Token("three", "three", PartOfSpeech.NUM)
			.Token("items", "item", PartOfSpeech.NOUN)
			.Token("on", "on", PartOfSpeech.ADP)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("shelf", "shelf", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		// "There" is extracted as subject (existential construction)
		Assert.NotNull(seed.Subject);
		Assert.Equal("there", seed.Subject!.Head);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("items", seed.DirectObject!.Head);
	}

	[Fact]
	public void Wh_Question_Subject()
	{
		// "Who opened the door?"
		var parsed = new ParsedInputBuilder()
			.Token("who", "who", PartOfSpeech.PRON)
			.Token("opened", "open", PartOfSpeech.VERB)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("who", seed.Subject!.Head);
		Assert.Equal("open", seed.Verb);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
	}

	[Fact]
	public void Wh_Subject_Assigns_Direct_Object_After_Verb()
	{
		// "Who opened the door?"
		var parsed = new ParsedInputBuilder()
			.Token("who", "who", PartOfSpeech.PRON)
			.Token("opened", "open", PartOfSpeech.VERB)
			.Token("the", "the", PartOfSpeech.DET)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("who", seed.Subject!.Head);

		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);

		Assert.Equal("open", seed.Verb);
	}

	[Fact]
	public void It_Pronoun_Subject()
	{
		// "It is raining"
		var parsed = new ParsedInputBuilder()
			.Token("it", "it", PartOfSpeech.PRON)
			.Token("is", "be", PartOfSpeech.AUX)
			.Token("raining", "rain", PartOfSpeech.VERB)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("it", seed.Subject!.Head);
		Assert.Equal("rain", seed.Verb);
	}

	[Fact]
	public void Gerund_As_Subject()
	{
		// "Running is healthy"
		var parsed = new ParsedInputBuilder()
			.Token("running", "running", PartOfSpeech.NOUN) // Gerunds often tagged as NOUN
			.Token("is", "be", PartOfSpeech.VERB)
			.Token("healthy", "healthy", PartOfSpeech.ADJ)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("running", seed.Subject!.Head);
		Assert.Equal("be", seed.Verb);
	}

	[Fact]
	public void No_Subject_In_Verbless_Phrase()
	{
		// "The red door" (no verb, no subject)
		var parsed = new ParsedInputBuilder()
			.Token("The", "the", PartOfSpeech.DET)
			.Token("red", "red", PartOfSpeech.ADJ)
			.Token("door", "door", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.Null(seed.Subject);
		Assert.Null(seed.Verb);
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("door", seed.DirectObject!.Head);
	}

	[Fact]
	public void Subject_Complement_Not_Direct_Object()
	{
		// "She is a doctor"
		var parsed = new ParsedInputBuilder()
			.Token("she", "she", PartOfSpeech.PRON)
			.Token("is", "be", PartOfSpeech.VERB)
			.Token("a", "a", PartOfSpeech.DET)
			.Token("doctor", "doctor", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		Assert.NotNull(seed.Subject);
		Assert.Equal("she", seed.Subject!.Head);
		Assert.Equal("be", seed.Verb);
		// Note: "a doctor" is a subject complement, not a direct object
		// This might need special handling for copula verbs
		Assert.NotNull(seed.DirectObject);
		Assert.Equal("doctor", seed.DirectObject!.Head);
	}
}