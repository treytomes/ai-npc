using Catalyst;

namespace LLM.NLP;

/// <summary>
/// Deterministic noun phrase extractor based on POS patterns.
/// </summary>
public sealed class PosBasedNounPhraseExtractor : INounPhraseExtractor
{
	private static readonly HashSet<string> _phrasalPrepStarters =
	[
		"out",
		"up",
		"down",
		"off",
		"away"
	];

	public NounPhrase? TryExtract(
		IReadOnlyList<ParsedToken> tokens,
		ref int index)
	{
		int start = index;

		var determiners = new List<string>();
		var modifiers = new List<string>();

		// 1. Determiners
		while (index < tokens.Count &&
			   tokens[index].Pos == PartOfSpeech.DET)
		{
			determiners.Add(tokens[index].Value);
			index++;
		}

		// 2. Adjectives
		while (index < tokens.Count &&
			   tokens[index].Pos == PartOfSpeech.ADJ)
		{
			modifiers.Add(tokens[index].Value);
			index++;
		}

		// 3. Head noun (required, but may be preceded by noun modifiers)
		if (index >= tokens.Count || tokens[index].Pos != PartOfSpeech.NOUN)
		{
			index = start;
			return null;
		}

		// Collect noun sequence
		var nounChain = new List<string>();
		while (index < tokens.Count && tokens[index].Pos == PartOfSpeech.NOUN)
		{
			nounChain.Add(tokens[index].Value);
			index++;
		}

		// Last noun is head, preceding nouns are modifiers
		var head = nounChain[^1];
		modifiers.AddRange(nounChain.Take(nounChain.Count - 1));

		var complements = new Dictionary<string, NounPhrase>();

		// 4. Prepositional complements (including phrasal preps)
		while (index < tokens.Count)
		{
			string? prep = null;

			// Handle "out of"
			if (index + 1 < tokens.Count &&
				tokens[index].Value.Equals("out", StringComparison.OrdinalIgnoreCase) &&
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
			complements: complements);
	}
}
