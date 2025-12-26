namespace LLM.NLP;

/// <summary>
/// A deterministic semantic seed describing player intent.
/// </summary>
public sealed class IntentSeed(
	string? verb,
	NounPhrase? subject,
	NounPhrase? directObject,
	NounPhrase? indirectObject,
	IReadOnlyDictionary<string, NounPhrase> prepositions
)
{
	public string? Verb { get; } = verb;
	public NounPhrase? Subject { get; } = subject;
	public NounPhrase? DirectObject { get; } = directObject;
	public NounPhrase? IndirectObject { get; } = indirectObject;
	public IReadOnlyDictionary<string, NounPhrase> Prepositions { get; } = prepositions;
}