namespace LLM.NLP;

/// <summary>
/// Represents normalized and structured information extracted
/// from raw user input for use by game logic and LLM narration.
/// </summary>
/// <param name="RawText">
/// The original raw input provided by the user.
/// </param>
/// <param name="NormalizedText">
/// The normalized text after basic cleanup (trimming, casing, etc.).
/// </param>
/// <param name="Tokens">
/// The individual tokens extracted from the input.
/// </param>
/// <param name="Lemmas">
/// Lemmatized tokens suitable for intent detection.
/// </param>
/// <param name="ParsedTokens">
/// </param>
public sealed record ParsedInput(
	string RawText,
	string NormalizedText,
	IReadOnlyList<string> Tokens,
	IReadOnlyList<string> Lemmas,
	IReadOnlyList<ParsedToken> ParsedTokens);