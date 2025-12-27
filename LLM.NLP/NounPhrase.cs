namespace LLM.NLP;

public sealed class NounPhrase(string head, IReadOnlyList<string> modifiers, IReadOnlyDictionary<string, NounPhrase> complements, string text, bool isCoordinated, IReadOnlyList<string> coordinatedHeads)
{
	public string Head { get; } = head;
	public IReadOnlyList<string> Modifiers { get; } = modifiers;
	public IReadOnlyDictionary<string, NounPhrase> Complements { get; } = complements;
	public string Text { get; } = text;

	public bool IsCoordinated { get; } = isCoordinated;
	public IReadOnlyList<string> CoordinatedHeads { get; } = coordinatedHeads;

	public bool IsQuestionWord =>
		IntentSeedExtractor.IsQuestionWord(Head);
}
