using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.FuzzySearch;
using NRules;

namespace LLM.Intent.Classification;

/// <summary>
/// Fuzzy matching on negative intent.
/// </summary>
internal sealed class NegativeIntentEvidenceProvider
	: IEvidenceProvider<Actor>
{
	public void Provide(ISession session, string utterance, Actor actor)
	{
		var negativePhrases = ShopkeeperNegativeIntentLexicon.Intents
			.SelectMany(kvp => kvp.Value.Select(p => (Intent: kvp.Key, Phrase: p)))
			.ToList();

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
	}
}
