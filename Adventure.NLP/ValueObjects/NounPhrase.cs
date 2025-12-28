namespace Adventure.NLP;

public sealed record NounPhrase(
	string Head,
	IReadOnlyList<string> Modifiers,
	IReadOnlyDictionary<string, NounPhrase> Complements,
	string Text,
	bool IsCoordinated,
	IReadOnlyList<string> CoordinatedHeads
)
{
	public bool IsQuestionWord => Head.IsQuestionWord();
}
