namespace LLM.NLP;

public sealed class IntentSeedExtractor
{
	private static readonly HashSet<string> _stopWords =
	[
		"the", "a", "an", "to", "of", "in", "on", "at", "with"
	];

	private static readonly HashSet<string> _commonVerbs =
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

		foreach (var lemma in input.Lemmas)
		{
			if (_stopWords.Contains(lemma))
				continue;

			if (verb == null && _commonVerbs.Contains(lemma))
			{
				verb = lemma;
				continue;
			}

			objects.Add(lemma);
		}

		return new IntentSeed(verb, objects);
	}
}
