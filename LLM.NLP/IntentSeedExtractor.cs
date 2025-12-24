using Catalyst;

namespace LLM.NLP;

/// <summary>
/// Extracts a deterministic intent seed from parsed input.
/// </summary>
internal sealed class IntentSeedExtractor : IIntentSeedExtractor
{
	#region Fields

	private readonly INounPhraseExtractor _nounPhrases;

	#endregion

	#region Constructors

	public IntentSeedExtractor(INounPhraseExtractor nounPhrases)
	{
		_nounPhrases = nounPhrases
			?? throw new ArgumentNullException(nameof(nounPhrases));
	}

	#endregion

	#region Methods

	public IntentSeed Extract(ParsedInput input)
	{
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		string? verb = null;
		NounPhrase? directObject = null;
		var prepositions = new Dictionary<string, NounPhrase>();

		var tokens = input.ParsedTokens;

		string? pendingPreposition = null;

		for (int i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];

			// 1. Verb (first verb wins)
			if (verb == null && token.Pos == PartOfSpeech.VERB)
			{
				verb = token.Lemma;
				continue;
			}

			// 2. Try noun phrase FIRST
			var phrase = _nounPhrases.TryExtract(tokens, ref i);
			if (phrase != null)
			{
				if (pendingPreposition != null)
				{
					// Intent-level prepositional phrase
					prepositions[pendingPreposition] = phrase;
					pendingPreposition = null;
				}
				else if (directObject == null)
				{
					directObject = phrase;
				}

				continue;
			}

			// 3. Preposition (only if not consumed by NP)
			if (token.Pos == PartOfSpeech.ADP)
			{
				pendingPreposition = token.Lemma;
				continue;
			}
		}

		return new IntentSeed(
			verb,
			directObject,
			prepositions);
	}

	#endregion
}
