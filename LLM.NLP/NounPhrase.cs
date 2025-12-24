namespace LLM.NLP;

public sealed class NounPhrase(string head, IReadOnlyList<string> modifiers, IReadOnlyDictionary<string, NounPhrase> complements, string text)
{
	public string Head { get; } = head;
	public IReadOnlyList<string> Modifiers { get; } = modifiers;
	public IReadOnlyDictionary<string, NounPhrase> Complements { get; } = complements;
	public string Text { get; } = text;
}
