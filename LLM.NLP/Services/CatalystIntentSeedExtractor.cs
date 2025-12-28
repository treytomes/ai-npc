using Catalyst;

namespace LLM.NLP.Services;

internal sealed class CatalystIntentSeedExtractor : IIntentSeedExtractor
{
	#region Fields

	private readonly INounPhraseExtractor _nounPhrases;

	#endregion

	#region Constructors

	public CatalystIntentSeedExtractor(INounPhraseExtractor nounPhrases)
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

		int mainVerbIndex = -1;
		int lastAuxIndex = -1;
		bool isInvertedStructure = false;
		bool hasWhSubject = false;

		#region First pass: find main verb

		for (int i = 0; i < tokens.Count; i++)
		{
			if (tokens[i].Pos == NlpPartOfSpeech.AuxiliaryVerb)
			{
				lastAuxIndex = i;

				if (i == 0 || (i == 1 && tokens[0].Pos == NlpPartOfSpeech.Pronoun))
					isInvertedStructure = true;

				bool hasVerbAfter = false;
				for (int j = i + 1; j < tokens.Count; j++)
				{
					if (tokens[j].Pos == NlpPartOfSpeech.Verb)
					{
						hasVerbAfter = true;
						break;
					}
				}

				if (!hasVerbAfter && verb == null)
				{
					verb = tokens[i].Lemma;
					mainVerbIndex = i;
					break;
				}
			}
			else if (tokens[i].Pos == NlpPartOfSpeech.Verb)
			{
				verb = tokens[i].Lemma;
				mainVerbIndex = i;
				break;
			}
		}

		#endregion

		#region Second pass: existing logic (unchanged)

		for (int i = 0; i < tokens.Count; i++)
		{
			if (i == mainVerbIndex)
				continue;

			if (tokens[i].Pos == NlpPartOfSpeech.AuxiliaryVerb && i != mainVerbIndex)
				continue;

			int phraseStartIndex = i;
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
					directObject ??= phrase;
				}
				else if (phraseStartIndex < mainVerbIndex)
				{
					// WH-subject question (e.g. "Who opened the door?")
					if (phraseStartIndex == 0 &&
						phrase.Head.IsQuestionWord() &&
						subject == null &&
						lastAuxIndex == -1)
					{
						subject = phrase;
						hasWhSubject = true;
					}
					// Normal subject assignment (e.g. "you" in "What do you have?")
					else if (subject == null && !phrase.Head.IsQuestionWord())
					{
						subject = phrase;
					}
					else if (directObject == null)
					{
						directObject = phrase;
					}
				}
				else
				{
					if (isInvertedStructure && subject == null && mainVerbIndex < 2)
						subject = phrase;
					else if (indirectObject == null && IsLikelyIndirectObject(phrase, tokens, i))
						indirectObject = phrase;
					else if (directObject == null)
						directObject = phrase;
				}

				i--;
				continue;
			}

			if (tokens[i].Pos == NlpPartOfSpeech.Adposition)
				pendingPreposition = tokens[i].Lemma;
		}

		#endregion

		#region FIX: WH-subject fallback (single, safe extraction)

		if (hasWhSubject && directObject == null && mainVerbIndex >= 0)
		{
			int i = mainVerbIndex + 1;
			if (i < tokens.Count)
			{
				var phrase = _nounPhrases.TryExtract(tokens, ref i);
				if (phrase != null)
					directObject = phrase;
			}
		}

		#endregion

		#region FIX: WH-object question normalization (auxiliary inversion)

		if (subject != null &&
			directObject == null &&
			subject.IsQuestionWord &&
			mainVerbIndex > 0 &&
			lastAuxIndex >= 0)
		{
			// Pattern: "What do you have ..."
			// If subject is WH and there's an AUX before the real subject,
			// reassign roles.

			// Find the noun phrase immediately after the auxiliary
			for (int i = lastAuxIndex + 1; i < mainVerbIndex; i++)
			{
				int temp = i;
				var phrase = _nounPhrases.TryExtract(tokens, ref temp);
				if (phrase != null && !phrase.IsQuestionWord)
				{
					directObject = subject;
					subject = phrase;
					break;
				}
			}
		}

		#endregion

		return new IntentSeed(
			verb,
			subject,
			directObject,
			indirectObject,
			prepositions);
	}

	private static bool IsLikelyIndirectObject(
		NounPhrase phrase,
		IReadOnlyList<ParsedToken> tokens,
		int currentIndex)
	{
		var pronouns = new HashSet<string> { "me", "you", "him", "her", "us", "them" };

		if (!pronouns.Contains(phrase.Head.ToLowerInvariant()))
			return false;

		for (int i = currentIndex; i < tokens.Count; i++)
		{
			if (tokens[i].Pos is NlpPartOfSpeech.Noun or NlpPartOfSpeech.Pronoun)
				return true;

			if (tokens[i].Pos is NlpPartOfSpeech.Adposition or NlpPartOfSpeech.Verb)
				break;
		}

		return false;
	}

	#endregion
}
