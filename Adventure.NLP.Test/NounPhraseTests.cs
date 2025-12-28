using Adventure.NLP.Services;
using Adventure.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.Test;

public sealed class NounPhraseTests
{
	private readonly IServiceProvider _provider;
	private readonly INounPhraseExtractor _extractor;

	public NounPhraseTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<INounPhraseExtractor>();
	}

	[Fact]
	public void Extracts_Adjective_Modified_Noun()
	{
		var parsed = new ParsedInputBuilder()
			.Token("rusty", pos: NlpPartOfSpeech.Adjective)
			.Token("old", pos: NlpPartOfSpeech.Adjective)
			.Token("knife", pos: NlpPartOfSpeech.Noun)
			.Build();

		var phrases = ExtractAll(parsed);

		Assert.Single(phrases);
		Assert.Equal("knife", phrases[0].Head);
		Assert.Equal(new[] { "rusty", "old" }, phrases[0].Modifiers);
	}

	[Fact]
	public void Extracts_Of_Complement()
	{
		var parsed = new ParsedInputBuilder()
			.Token("loaf", pos: NlpPartOfSpeech.Noun)
			.Token("of", pos: NlpPartOfSpeech.Adposition)
			.Token("bread", pos: NlpPartOfSpeech.Noun)
			.Build();

		var phrases = ExtractAll(parsed);

		var np = phrases.Single();
		Assert.Equal("loaf", np.Head);
		Assert.Equal("bread", np.Complements["of"].Head);
	}

	[Fact]
	public void Extracts_compound_nouns()
	{
		var parsed = new ParsedInputBuilder()
			.Token("toggle", pos: NlpPartOfSpeech.Noun)
			.Token("document", pos: NlpPartOfSpeech.Noun)
			.Build();

		var phrases = ExtractAll(parsed);

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
