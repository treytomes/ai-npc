namespace LLM.NLP.Services;

/// <summary>
/// Extracts noun phrases from a sequence of parsed tokens.
/// </summary>
public interface INounPhraseExtractor
{
	/// <summary>
	/// Attempts to extract a noun phrase starting at the given token index.
	/// Returns null if no noun phrase starts here.
	/// </summary>
	NounPhrase? TryExtract(
		IReadOnlyList<ParsedToken> tokens,
		ref int index);
}
