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
		NounPhrase? subject = null;
		NounPhrase? directObject = null;
		NounPhrase? indirectObject = null;
		var prepositions = new Dictionary<string, NounPhrase>();

		var tokens = input.ParsedTokens;
		string? pendingPreposition = null;

		// First pass: find the main verb and last auxiliary before it
		int mainVerbIndex = -1;
		int lastAuxIndex = -1;

		for (int i = 0; i < tokens.Count; i++)
		{
			if (tokens[i].Pos == PartOfSpeech.AUX)
			{
				lastAuxIndex = i;
			}
			else if (tokens[i].Pos == PartOfSpeech.VERB)
			{
				verb = tokens[i].Lemma;
				mainVerbIndex = i;
				break;
			}
		}

		// Second pass: process noun phrases based on position
		for (int i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];

			// Skip the verb itself
			if (i == mainVerbIndex)
				continue;

			// Skip auxiliaries
			if (token.Pos == PartOfSpeech.AUX)
				continue;

			// Store the current position before TryExtract
			int phraseStartIndex = i;

			// Try to extract noun phrase
			var phrase = _nounPhrases.TryExtract(tokens, ref i);
			if (phrase != null)
			{
				if (pendingPreposition != null)
				{
					prepositions[pendingPreposition] = phrase;
					pendingPreposition = null;
				}
				else if (mainVerbIndex == -1)
				{
					// No verb case - first NP is direct object
					if (directObject == null)
						directObject = phrase;
				}
				else if (phraseStartIndex < mainVerbIndex)
				{
					// Use phraseStartIndex for position check
					if (lastAuxIndex != -1 && phraseStartIndex > lastAuxIndex)
					{
						// Between auxiliary and main verb = subject
						subject = phrase;
					}
					else if (lastAuxIndex == -1)
					{
						// No auxiliary, before verb = subject
						subject = phrase;
					}
					else
					{
						// Before auxiliary = likely question word (direct object)
						if (directObject == null)
							directObject = phrase;
					}
				}
				else
				{
					// After verb - could be indirect or direct object
					if (indirectObject == null && IsLikelyIndirectObject(phrase, tokens, i))
					{
						indirectObject = phrase;
					}
					else if (directObject == null)
					{
						directObject = phrase;
					}
				}

				// Important: i has already been advanced by TryExtract
				i--; // Decrement because the loop will increment
				continue;
			}

			// Handle prepositions
			if (token.Pos == PartOfSpeech.ADP)
			{
				pendingPreposition = token.Lemma;
			}
		}

		return new IntentSeed(
			verb,
			subject,
			directObject,
			indirectObject,
			prepositions);
	}

	private bool IsLikelyIndirectObject(NounPhrase phrase, IReadOnlyList<ParsedToken> tokens, int currentIndex)
	{
		// Simple heuristic: pronouns like "me", "you", "him", "her", "us", "them" 
		// immediately after a verb are often indirect objects
		var indirectObjectPronouns = new HashSet<string>
		{
			"me", "you", "him", "her", "us", "them"
		};

		if (indirectObjectPronouns.Contains(phrase.Head.ToLower()))
		{
			// Check if there's another NP coming (which would be the direct object)
			for (int i = currentIndex; i < tokens.Count; i++)
			{
				if (tokens[i].Pos == PartOfSpeech.NOUN ||
					tokens[i].Pos == PartOfSpeech.PRON)
				{
					return true;
				}
				// Stop looking if we hit a preposition or verb
				if (tokens[i].Pos == PartOfSpeech.ADP ||
					tokens[i].Pos == PartOfSpeech.VERB)
				{
					break;
				}
			}
		}

		return false;
	}

	#endregion
}