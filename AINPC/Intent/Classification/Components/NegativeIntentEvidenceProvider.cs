using AINPC.Entities;
using AINPC.Intent.Classification.Facts;
using AINPC.Intent.FuzzySearch;
using AINPC.Intent.Lexicons;
using NRules;

namespace AINPC.Intent.Classification;

/// <summary>
/// Fuzzy matching on negative intent.
/// </summary>
internal sealed class NegativeIntentEvidenceProvider(IIntentLexiconFactory intentLexiconFactory)
	: IEvidenceProvider<Actor>
{
	public void Provide(ISession session, string utterance, Actor actor)
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
	}
}
