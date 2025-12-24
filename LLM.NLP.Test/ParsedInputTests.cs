using Spectre.Console;
using Xunit;

namespace LLM.NLP.Test;

/// <summary>
/// Verifies basic construction and invariants of the ParsedInput model.
/// </summary>
public sealed class ParsedInputTests : IDisposable
{
	public ParsedInputTests()
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]ParsedInput — Model Invariants[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		AnsiConsole.Write(
			new Rule("[dim]End ParsedInput Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

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

		RenderParsedInput(parsed);

		// ASSERT
		Assert.Equal(raw, parsed.RawText);
		Assert.Equal(normalized, parsed.NormalizedText);
		Assert.Equal(tokens, parsed.Tokens);
		Assert.Equal(lemmatizedTokens, parsed.Lemmas);
	}

	private static void RenderParsedInput(ParsedInput parsed)
	{
		AnsiConsole.MarkupLine("[grey]ParsedInput Debug Output[/]");

		var table = new Table()
			.AddColumn("Property")
			.AddColumn("Value");

		table.AddRow("RawText", parsed.RawText);
		table.AddRow("NormalizedText", parsed.NormalizedText);
		table.AddRow("Tokens", string.Join(", ", parsed.Tokens));
		table.AddRow("Lemmas", string.Join(", ", parsed.Lemmas));

		AnsiConsole.Write(table);
	}
}
