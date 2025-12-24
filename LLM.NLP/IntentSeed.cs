namespace LLM.NLP;

/// <summary>
/// A deterministic semantic seed describing player intent.
/// </summary>
public sealed class IntentSeed(
	string? verb,
	string? directObject,
	IReadOnlyDictionary<string, string> prepositions
)
{
	public string? Verb { get; } = verb;
	public string? DirectObject { get; } = directObject;
	public IReadOnlyDictionary<string, string> Prepositions { get; } = prepositions;
}