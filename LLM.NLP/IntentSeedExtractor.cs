using Catalyst;

namespace LLM.NLP;

/// <summary>
/// Extracts a deterministic intent seed from parsed input.
/// </summary>
public sealed class IntentSeedExtractor
{
	private static readonly HashSet<string> _stopWords =
	[
		"the", "a", "an"
	];

	public IntentSeed Extract(ParsedInput input)
	{
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		string? verb = null;
		string? directObject = null;
		var prepositions = new Dictionary<string, string>();

		ParsedToken? lastPrep = null;

		foreach (var token in input.ParsedTokens)
		{
			if (_stopWords.Contains(token.Lemma))
				continue;

			// 1. Verb
			if (verb == null && token.Pos == PartOfSpeech.VERB)
			{
				verb = token.Lemma;
				continue;
			}

			// 2. Preposition
			if (token.Pos == PartOfSpeech.ADP)
			{
				lastPrep = token;
				continue;
			}

			// 3. Noun handling
			if (token.Pos == PartOfSpeech.NOUN)
			{
				if (lastPrep != null)
				{
					// Attach noun to preposition
					prepositions[lastPrep.Lemma] = token.Lemma;
					lastPrep = null;
				}
				else if (directObject == null)
				{
					directObject = token.Lemma;
				}
			}
		}

		return new IntentSeed(
			verb,
			directObject,
			prepositions);
	}
}
