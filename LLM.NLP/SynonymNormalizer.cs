namespace LLM.NLP;

public class SynonymNormalizer : IIntentPipelineStep
{
	#region Fields

	private readonly IReadOnlyDictionary<string, string> _synonyms;

	#endregion

	#region Constructors

	public SynonymNormalizer(IReadOnlyDictionary<string, string> synonyms)
	{
		_synonyms = synonyms ?? throw new ArgumentNullException(nameof(synonyms));
	}

	#endregion

	#region Methods

	/// <summary>
	/// Loads synonym mappings from a JSON file.
	/// JSON format: { "normalized": ["synonym1", "synonym2", ...], ... }
	/// </summary>
	public static SynonymNormalizer FromJsonFile(string filePath)
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"Synonym file not found: {filePath}");

		var json = File.ReadAllText(filePath);
		return FromJson(json);
	}

	/// <summary>
	/// Loads synonym mappings from a JSON string.
	/// JSON format: { "normalized": ["synonym1", "synonym2", ...], ... }
	/// </summary>
	public static SynonymNormalizer FromJson(string json)
	{
		var groups = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
			?? throw new InvalidOperationException("Failed to deserialize synonym JSON");

		var synonymMap = new Dictionary<string, string>();

		foreach (var (normalized, synonyms) in groups)
		{
			// Map the normalized word to itself
			synonymMap[normalized] = normalized;

			// Map each synonym to the normalized form
			foreach (var synonym in synonyms)
			{
				synonymMap[synonym] = normalized;
			}
		}

		return new SynonymNormalizer(synonymMap);
	}

	public IntentSeed Process(IntentSeed seed)
	{
		var normalizedVerb = NormalizeToken(seed.Verb);
		var normalizedDirectObject = NormalizeNounPhrase(seed.DirectObject);
		var normalizedPrepositions = NormalizePrepositions(seed.Prepositions);

		return new IntentSeed(
			normalizedVerb,
			normalizedDirectObject,
			normalizedPrepositions
		);
	}

	private string? NormalizeToken(string? token)
	{
		if (token == null)
			return null;

		return _synonyms.TryGetValue(token, out var normalized)
			? normalized
			: token;
	}

	private NounPhrase? NormalizeNounPhrase(NounPhrase? nounPhrase)
	{
		if (nounPhrase == null)
			return null;

		var normalizedHead = NormalizeToken(nounPhrase.Head) ?? nounPhrase.Head;
		var normalizedComplements = NormalizePrepositions(nounPhrase.Complements);

		return new NounPhrase(
			normalizedHead,
			nounPhrase.Modifiers, // Modifiers stay as-is
			normalizedComplements,
			nounPhrase.Text
		);
	}

	private IReadOnlyDictionary<string, NounPhrase> NormalizePrepositions(
		IReadOnlyDictionary<string, NounPhrase> prepositions)
	{
		if (prepositions.Count == 0)
			return prepositions;

		var normalized = new Dictionary<string, NounPhrase>();

		foreach (var (prep, nounPhrase) in prepositions)
		{
			// Normalize the preposition key itself (e.g., "with" -> "using")
			var normalizedPrep = NormalizeToken(prep) ?? prep;
			var normalizedNounPhrase = NormalizeNounPhrase(nounPhrase) ?? nounPhrase;

			normalized[normalizedPrep] = normalizedNounPhrase;
		}

		return normalized;
	}

	#endregion
}