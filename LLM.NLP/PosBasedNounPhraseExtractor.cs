using Catalyst;

namespace LLM.NLP;

/// <summary>
/// Deterministic noun phrase extractor based on POS patterns.
/// </summary>
public sealed class PosBasedNounPhraseExtractor : INounPhraseExtractor
{
	private static readonly HashSet<PartOfSpeech> NominalPos =
	[
		PartOfSpeech.NOUN,
		PartOfSpeech.PRON,  // Add pronouns
        PartOfSpeech.PROPN  // Add proper nouns
    ];

	public NounPhrase? TryExtract(
		IReadOnlyList<ParsedToken> tokens,
		ref int index)
	{
		int start = index;

		// Special case: Handle pronouns as simple noun phrases
		if (index < tokens.Count && tokens[index].Pos == PartOfSpeech.PRON)
		{
			var pronoun = tokens[index];
			index++;

			// Check for relative clauses (e.g., "what you have")
			if (IsRelativePronoun(pronoun.Lemma) && index < tokens.Count)
			{
				return TryExtractRelativeClause(tokens, ref index, pronoun.Value);
			}

			return new NounPhrase(
				head: pronoun.Value,
				modifiers: [],
				complements: new Dictionary<string, NounPhrase>(),
				text: pronoun.Value
			);
		}

		var determiners = new List<string>();
		var modifiers = new List<string>();

		// 1. Determiners
		while (index < tokens.Count && tokens[index].Pos == PartOfSpeech.DET)
		{
			determiners.Add(tokens[index].Value);
			index++;
		}

		// 2. Adjectives
		while (index < tokens.Count && tokens[index].Pos == PartOfSpeech.ADJ)
		{
			modifiers.Add(tokens[index].Value);
			index++;
		}

		// 3. Head nominal (noun, pronoun, or proper noun)
		if (index >= tokens.Count || !NominalPos.Contains(tokens[index].Pos))
		{
			index = start;
			return null;
		}

		// Collect nominal sequence
		var nominalChain = new List<string>();
		while (index < tokens.Count && NominalPos.Contains(tokens[index].Pos))
		{
			nominalChain.Add(tokens[index].Value);
			index++;
		}

		var head = nominalChain[^1];
		modifiers.AddRange(nominalChain.Take(nominalChain.Count - 1));

		var complements = new Dictionary<string, NounPhrase>();

		// 4. Prepositional complements (including phrasal preps)
		while (index < tokens.Count)
		{
			string? prep = null;

			// Handle "out of" - check both when "out" is ADJ or ADP
			if (index + 1 < tokens.Count &&
				tokens[index].Value.Equals("out", StringComparison.OrdinalIgnoreCase) &&
				(tokens[index].Pos == PartOfSpeech.ADJ || tokens[index].Pos == PartOfSpeech.ADP) &&
				tokens[index + 1].Pos == PartOfSpeech.ADP &&
				tokens[index + 1].Value.Equals("of", StringComparison.OrdinalIgnoreCase))
			{
				prep = "out of";
				index += 2;
			}
			// Single-word preposition
			else if (tokens[index].Pos == PartOfSpeech.ADP)
			{
				prep = tokens[index].Lemma;
				index++;
			}
			else
			{
				break;
			}

			var complement = TryExtract(tokens, ref index);
			if (complement == null)
				break;

			complements[prep] = complement;
		}

		// Build text
		var textParts = new List<string>();
		textParts.AddRange(determiners);
		textParts.AddRange(modifiers);
		textParts.Add(head);

		foreach (var (prep, np) in complements)
		{
			textParts.Add(prep);
			textParts.Add(np.Text);
		}

		return new NounPhrase(
			text: string.Join(" ", textParts),
			head: head,
			modifiers: determiners.Concat(modifiers).ToList(),
			complements: complements
		);
	}

	private bool IsRelativePronoun(string lemma)
	{
		return lemma is "what" or "which" or "that" or "who" or "whom" or "whose";
	}

	private NounPhrase? TryExtractRelativeClause(
	IReadOnlyList<ParsedToken> tokens,
	ref int index,
	string relativePronoun)
	{
		// Look ahead to determine if this is truly a relative clause
		// In questions like "What do you have?", "what" is interrogative, not relative

		// Check if the next few tokens form a question pattern (AUX + PRON + VERB)
		if (index < tokens.Count - 2 &&
			tokens[index].Pos == PartOfSpeech.AUX &&
			tokens[index + 1].Pos == PartOfSpeech.PRON &&
			index + 2 < tokens.Count &&
			tokens[index + 2].Pos == PartOfSpeech.VERB)
		{
			// This is a question pattern, not a relative clause
			// Just return the pronoun itself
			return new NounPhrase(
				head: relativePronoun,
				modifiers: [],
				complements: new Dictionary<string, NounPhrase>(),
				text: relativePronoun
			);
		}

		var clauseTokens = new List<string> { relativePronoun };
		int clauseStart = index;

		// Original relative clause extraction logic...
		while (index < tokens.Count)
		{
			var token = tokens[index];

			// Stop at sentence-ending punctuation or coordinating conjunctions
			if (token.Pos == PartOfSpeech.PUNCT ||
				(token.Pos == PartOfSpeech.CCONJ && clauseTokens.Count > 1))
			{
				break;
			}

			clauseTokens.Add(token.Value);
			index++;

			// Stop after a verb + its complements if we find a preposition at intent level
			if (token.Pos == PartOfSpeech.VERB &&
				index < tokens.Count &&
				tokens[index].Pos == PartOfSpeech.ADP)
			{
				// Check if this preposition starts a new phrase at intent level
				// by looking ahead
				int lookahead = index + 1;
				if (lookahead < tokens.Count &&
					NominalPos.Contains(tokens[lookahead].Pos))
				{
					break;
				}
			}
		}

		if (clauseTokens.Count > 1)
		{
			return new NounPhrase(
				head: relativePronoun,
				modifiers: [],
				complements: new Dictionary<string, NounPhrase>(),
				text: string.Join(" ", clauseTokens)
			);
		}

		index = clauseStart;
		return null;
	}
}