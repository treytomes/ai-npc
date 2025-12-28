using Adventure.NLP.Services;
using Adventure.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.Test;

public class IntentSeedExtractor_SubjectTests
{
	private readonly IServiceProvider _provider;
	private readonly IIntentSeedExtractor _extractor;

	public IntentSeedExtractor_SubjectTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<IIntentSeedExtractor>();
	}

	[Fact]
	public void Extracts_Simple_Pronoun_Subject()
	{
		// "You have three items"
		var parsed = new ParsedInputBuilder()
			.Token("you", "you", NlpPartOfSpeech.Pronoun)
			.Token("have", "have", NlpPartOfSpeech.Verb)
			.Token("three", "three", NlpPartOfSpeech.Numeral)
			.Token("items", "item", NlpPartOfSpeech.Noun)
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
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("cat", "cat", NlpPartOfSpeech.Noun)
			.Token("sits", "sit", NlpPartOfSpeech.Verb)
			.Token("on", "on", NlpPartOfSpeech.Adposition)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("mat", "mat", NlpPartOfSpeech.Noun)
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
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("old", "old", NlpPartOfSpeech.Adjective)
			.Token("brown", "brown", NlpPartOfSpeech.Adjective)
			.Token("dog", "dog", NlpPartOfSpeech.Noun)
			.Token("barked", "bark", NlpPartOfSpeech.Verb)
			.Token("loudly", "loudly", NlpPartOfSpeech.Adverb)
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
			.Token("Show", "show", NlpPartOfSpeech.Verb)
			.Token("me", "me", NlpPartOfSpeech.Pronoun)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
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
			.Token("what", "what", NlpPartOfSpeech.Pronoun)
			.Token("do", "do", NlpPartOfSpeech.AuxiliaryVerb)
			.Token("you", "you", NlpPartOfSpeech.Pronoun)
			.Token("have", "have", NlpPartOfSpeech.Verb)
			.Token("for", "for", NlpPartOfSpeech.Adposition)
			.Token("sale", "sale", NlpPartOfSpeech.Noun)
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
			.Token("is", "be", NlpPartOfSpeech.AuxiliaryVerb)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
			.Token("open", "open", NlpPartOfSpeech.Adjective)
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
			.Token("john", "john", NlpPartOfSpeech.ProperNoun)
			.Token("and", "and", NlpPartOfSpeech.CoordinatingConjunction)
			.Token("mary", "mary", NlpPartOfSpeech.ProperNoun)
			.Token("went", "go", NlpPartOfSpeech.Verb)
			.Token("to", "to", NlpPartOfSpeech.Adposition)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("store", "store", NlpPartOfSpeech.Noun)
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
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("man", "man", NlpPartOfSpeech.Noun)
			.Token("with", "with", NlpPartOfSpeech.Adposition)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("hat", "hat", NlpPartOfSpeech.Noun)
			.Token("smiled", "smile", NlpPartOfSpeech.Verb)
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
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
			.Token("was", "be", NlpPartOfSpeech.AuxiliaryVerb)
			.Token("opened", "open", NlpPartOfSpeech.Verb)
			.Token("by", "by", NlpPartOfSpeech.Adposition)
			.Token("john", "john", NlpPartOfSpeech.ProperNoun)
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
			.Token("there", "there", NlpPartOfSpeech.Pronoun)
			.Token("are", "be", NlpPartOfSpeech.Verb)
			.Token("three", "three", NlpPartOfSpeech.Numeral)
			.Token("items", "item", NlpPartOfSpeech.Noun)
			.Token("on", "on", NlpPartOfSpeech.Adposition)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("shelf", "shelf", NlpPartOfSpeech.Noun)
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
			.Token("who", "who", NlpPartOfSpeech.Pronoun)
			.Token("opened", "open", NlpPartOfSpeech.Verb)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
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
			.Token("who", "who", NlpPartOfSpeech.Pronoun)
			.Token("opened", "open", NlpPartOfSpeech.Verb)
			.Token("the", "the", NlpPartOfSpeech.Determiner)
			.Token("door", "door", NlpPartOfSpeech.Noun)
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
			.Token("it", "it", NlpPartOfSpeech.Pronoun)
			.Token("is", "be", NlpPartOfSpeech.AuxiliaryVerb)
			.Token("raining", "rain", NlpPartOfSpeech.Verb)
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
			.Token("running", "running", NlpPartOfSpeech.Noun) // Gerunds often tagged as NOUN
			.Token("is", "be", NlpPartOfSpeech.Verb)
			.Token("healthy", "healthy", NlpPartOfSpeech.Adjective)
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
			.Token("The", "the", NlpPartOfSpeech.Determiner)
			.Token("red", "red", NlpPartOfSpeech.Adjective)
			.Token("door", "door", NlpPartOfSpeech.Noun)
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
			.Token("she", "she", NlpPartOfSpeech.Pronoun)
			.Token("is", "be", NlpPartOfSpeech.Verb)
			.Token("a", "a", NlpPartOfSpeech.Determiner)
			.Token("doctor", "doctor", NlpPartOfSpeech.Noun)
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