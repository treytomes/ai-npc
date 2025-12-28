namespace LLM.NLP;

/// <summary>
/// Represents a normalized token with linguistic metadata.
/// </summary>
public sealed record ParsedToken(
	string Value,
	string Lemma,
	NlpPartOfSpeech Pos);
