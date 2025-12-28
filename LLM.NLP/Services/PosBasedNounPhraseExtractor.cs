using Catalyst;

namespace LLM.NLP.Services;

/// <summary>
/// Deterministic noun phrase extractor based on POS patterns.
/// </summary>
public sealed class PosBasedNounPhraseExtractor : INounPhraseExtractor
{
	private static readonly HashSet<PartOfSpeech> NominalPos =
	[
		PartOfSpeech.NOUN,
		PartOfSpeech.PRON,
		PartOfSpeech.PROPN
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
				Head: pronoun.Value,
				Modifiers: [],
				Complements: new Dictionary<string, NounPhrase>(),
				Text: pronoun.Value,
				IsCoordinated: false,
				CoordinatedHeads: []
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

		// 3. Head nominal (noun, pronoun, or proper noun) with coordination support
		if (index >= tokens.Count || !NominalPos.Contains(tokens[index].Pos))
		{
			index = start;
			return null;
		}

		// Collect nominal sequence with coordination
		var coordinatedHeads = new List<string>();
		var allNominals = new List<string>();
		bool isCoordinated = false;

		while (index < tokens.Count)
		{
			if (NominalPos.Contains(tokens[index].Pos))
			{
				var currentNominal = tokens[index].Value;
				allNominals.Add(currentNominal);
				coordinatedHeads.Add(currentNominal);
				index++;

				// Check for coordination
				if (index < tokens.Count && tokens[index].Pos == PartOfSpeech.CCONJ)
				{
					isCoordinated = true;
					allNominals.Add(tokens[index].Value); // Add the conjunction
					index++;
					continue; // Look for more coordinated elements
				}
				else if (!isCoordinated)
				{
					// No coordination, continue collecting compound nouns
					continue;
				}
				else
				{
					// We've finished a coordinated sequence
					break;
				}
			}
			else
			{
				break;
			}
		}

		// Determine head and modifiers
		string head;
		List<string> nominalModifiers;

		if (isCoordinated)
		{
			// For coordinated phrases, use the last coordinated element as head
			head = coordinatedHeads[^1];
			nominalModifiers = new List<string>();
		}
		else
		{
			// For non-coordinated phrases, last nominal is head, others are modifiers
			head = allNominals[^1];
			nominalModifiers = allNominals.Take(allNominals.Count - 1).ToList();
		}

		modifiers.AddRange(nominalModifiers);

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

		if (isCoordinated)
		{
			// For coordinated phrases, include all parts
			textParts.AddRange(allNominals);
		}
		else
		{
			textParts.AddRange(modifiers);
			textParts.Add(head);
		}

		foreach (var (prep, np) in complements)
		{
			textParts.Add(prep);
			textParts.Add(np.Text);
		}

		return new NounPhrase(
			Text: string.Join(" ", textParts),
			Head: head,
			Modifiers: determiners.Concat(modifiers).ToList(),
			Complements: complements,
			IsCoordinated: isCoordinated,
			CoordinatedHeads: isCoordinated ? coordinatedHeads : []
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
				Head: relativePronoun,
				Modifiers: [],
				Complements: new Dictionary<string, NounPhrase>(),
				Text: relativePronoun,
				IsCoordinated: false,
				CoordinatedHeads: []
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
				Head: relativePronoun,
				Modifiers: [],
				Complements: new Dictionary<string, NounPhrase>(),
				Text: string.Join(" ", clauseTokens),
				IsCoordinated: false,
				CoordinatedHeads: []
			);
		}

		index = clauseStart;
		return null;
	}
}