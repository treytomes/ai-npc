using Adventure.Entities;
using Adventure.Intent.Classification.Facts;
using Adventure.Intent.FuzzySearch;
using Adventure.Intent.Lexicons;
using NRules;

namespace Adventure.Intent.Classification;

/// <summary>
/// Fuzzy matching on negative intent.
/// </summary>
internal sealed class NegativeIntentEvidenceProvider(IIntentLexiconFactory intentLexiconFactory)
	: IEvidenceProvider<Actor>
{
	public async Task ProvideAsync(ISession session, string utterance, Actor actor)
	{
		var negativePhrases = intentLexiconFactory.GetLexicon("negative_intent_lexicon.json").Intents
			.SelectMany(i => i.Patterns.Select(p => (Intent: i.Name, Phrase: p)));

		var negativeEngine = negativePhrases
			.Select(p => p.Phrase)
			.ToSearchEngine(new SearchOptions
			{
				MinimumSimilarity = 0.3
			});

		var negativeResults = negativeEngine
			.SearchAsync(utterance)
			.GetAwaiter()
			.GetResult();

		foreach (var result in negativeResults)
		{
			var intent = negativePhrases
				.First(p => p.Phrase.Equals(result.Text, StringComparison.OrdinalIgnoreCase))
				.Intent;

			session.Insert(new NegativeIntentHint(intent, result.Score));
		}

		await Task.CompletedTask;
	}
}
