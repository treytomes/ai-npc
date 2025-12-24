using Spectre.Console;
using Xunit;

namespace LLM.NLP.Test;

/// <summary>
/// Verifies basic construction and invariants of the ParsedInput model.
/// </summary>
public class ParsedInputTests
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
		var parsed = new ParsedInput(raw, normalized, tokens, lemmatizedTokens);

		AnsiConsole.MarkupLine("[grey]ParsedInput Debug Output[/]");
		AnsiConsole.Write(new Table()
			.AddColumn("Property")
			.AddColumn("Value")
			.AddRow("RawText", parsed.RawText)
			.AddRow("NormalizedText", parsed.NormalizedText)
			.AddRow("Tokens", string.Join(", ", parsed.Tokens)));

		// ASSERT
		Assert.Equal(raw, parsed.RawText);
		Assert.Equal(normalized, parsed.NormalizedText);
		Assert.Equal(tokens, parsed.Tokens);
		Assert.Equal(lemmatizedTokens, parsed.Lemmas);
	}
}
