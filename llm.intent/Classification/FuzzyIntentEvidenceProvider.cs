using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.FuzzySearch;
using NRules;

namespace LLM.Intent.Classification;

/// <summary>
/// This will attempt to make a fuzzy guess on what the user intended based on what they said.
/// </summary>
internal sealed class FuzzyIntentEvidenceProvider
	: IEvidenceProvider<Actor>
{
	public void Provide(ISession session, string utterance, Actor actor)
	{
		var intentPhrases = ShopkeeperIntentLexicon.Intents
			.SelectMany(kvp => kvp.Value.Select(p => (Intent: kvp.Key, Phrase: p)))
			.ToList();

		var intentEngine = intentPhrases
			.Select(p => p.Phrase)
			.ToSearchEngine(new SearchOptions
			{
				MinimumSimilarity = 0.25,
			});

		var intentResults = intentEngine
			.SearchAsync(utterance)
			.GetAwaiter()
			.GetResult();

		foreach (var result in intentResults)
		{
			var matchedIntent = intentPhrases
				.First(p => p.Phrase.Equals(result.Text, StringComparison.OrdinalIgnoreCase))
				.Intent;

			var hint = new FuzzyIntentHint(matchedIntent, result.Score);
			// Console.WriteLine($"Intent hint: {hint.Intent} ({hint.Confidence:0.00})");

			session.Insert(hint);
		}
	}
}
