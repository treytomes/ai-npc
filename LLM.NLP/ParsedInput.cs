namespace LLM.NLP;

/// <summary>
/// Represents normalized and structured information extracted
/// from raw user input for use by game logic and LLM narration.
/// </summary>
public sealed class ParsedInput(
	string rawText,
	string normalizedText,
	IReadOnlyList<string> tokens,
	IReadOnlyList<string> lemmas,
	IReadOnlyList<ParsedToken> parsedTokens)
{
	/// <summary>
	/// The original raw input provided by the user.
	/// </summary>
	public string RawText { get; } = rawText;

	/// <summary>
	/// The normalized text after basic cleanup (trimming, casing, etc.).
	/// </summary>
	public string NormalizedText { get; } = normalizedText;

	/// <summary>
	/// The individual tokens extracted from the input.
	/// </summary>
	public IReadOnlyList<string> Tokens { get; } = tokens;

	/// <summary>
	/// Lemmatized tokens suitable for intent detection.
	/// </summary>
	public IReadOnlyList<string> Lemmas { get; } = lemmas;

	public IReadOnlyList<ParsedToken> ParsedTokens { get; } = parsedTokens;
}
