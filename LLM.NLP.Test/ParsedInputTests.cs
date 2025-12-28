namespace LLM.NLP.Test;

/// <summary>
/// Verifies basic construction and invariants of the ParsedInput model.
/// </summary>
public sealed class ParsedInputTests
{
	[Fact]
	public void ParsedInput_StoresProvidedValues()
	{
		// ARRANGE
		var raw = "Opened the doors.";
		var normalized = "opened the doors";
		var tokens = new[] { "opened", "the", "doors" };
		var lemmatizedTokens = new[] { "open", "the", "door" };

		// ACT
		var parsed = new ParsedInput(
			raw,
			normalized,
			tokens,
			lemmatizedTokens,
			[]);

		// ASSERT
		Assert.Equal(raw, parsed.RawText);
		Assert.Equal(normalized, parsed.NormalizedText);
		Assert.Equal(tokens, parsed.Tokens);
		Assert.Equal(lemmatizedTokens, parsed.Lemmas);
	}
}
