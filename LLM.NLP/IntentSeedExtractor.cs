using Catalyst;

namespace LLM.NLP;

public sealed class IntentSeedExtractor
{
	private static readonly HashSet<string> _stopWords =
	[
		"the", "a", "an", "to", "of", "in", "on", "at", "with"
	];

	private static readonly HashSet<string> _fallbackVerbs =
	[
		"open", "close", "take", "drop", "look", "use", "go",
		"move", "attack", "talk", "pick", "examine"
	];

	public IntentSeed Extract(ParsedInput input)
	{
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		string? verb = null;
		var objects = new List<string>();

		// 1. POS-based verb detection
		var verbToken = input.ParsedTokens
			.FirstOrDefault(t => t.Pos == PartOfSpeech.VERB);

		if (verbToken != null)
		{
			verb = verbToken.Lemma;
		}

		// 2. Object extraction
		foreach (var token in input.ParsedTokens)
		{
			if (_stopWords.Contains(token.Lemma))
				continue;

			if (token.Pos == PartOfSpeech.VERB)
				continue;

			objects.Add(token.Lemma);
		}

		// 3. Fallback verb detection
		if (verb == null)
		{
			verb = input.Lemmas.FirstOrDefault(l => _fallbackVerbs.Contains(l));
			// TODO: I might want to throw an error here instead.
		}

		return new IntentSeed(verb, objects);
	}
}
