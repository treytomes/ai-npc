namespace LLM.NLP;

/// <summary>
/// A deterministic semantic seed describing player intent.
/// </summary>
public sealed class IntentSeed(
	string? verb,
	NounPhrase? directObject,
	IReadOnlyDictionary<string, NounPhrase> prepositions
)
{
	public string? Verb { get; } = verb;
	public NounPhrase? DirectObject { get; } = directObject;
	public IReadOnlyDictionary<string, NounPhrase> Prepositions { get; } = prepositions;
}