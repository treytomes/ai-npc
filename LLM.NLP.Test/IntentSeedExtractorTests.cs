using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Tests for extracting intent seeds (verb + objects) from parsed input.
/// Includes Spectre.Console output to aid debugging and understanding.
/// </summary>
public class IntentSeedExtractorTests
{
	[Fact]
	public void Extractor_Finds_Verb_And_Object()
	{
		// ARRANGE
		var parsed = new ParsedInput(
			rawText: "open the door",
			normalizedText: "open the door",
			tokens: ["open", "the", "door"],
			lemmas: ["open", "the", "door"]);

		var extractor = new IntentSeedExtractor();

		// ACT
		var seed = extractor.Extract(parsed);

		RenderSeed("open the door", parsed, seed);

		// ASSERT
		Assert.Equal("open", seed.Verb);
		Assert.Contains("door", seed.Objects);
	}

	[Fact]
	public void Extractor_Handles_No_Object()
	{
		// ARRANGE
		var parsed = new ParsedInput(
			rawText: "look",
			normalizedText: "look",
			tokens: ["look"],
			lemmas: ["look"]);

		var extractor = new IntentSeedExtractor();

		// ACT
		var seed = extractor.Extract(parsed);

		RenderSeed("look", parsed, seed);

		// ASSERT
		Assert.Equal("look", seed.Verb);
		Assert.Empty(seed.Objects);
	}

	[Fact]
	public void Extractor_Handles_No_Verb()
	{
		// ARRANGE
		var parsed = new ParsedInput(
			rawText: "the door",
			normalizedText: "the door",
			tokens: ["the", "door"],
			lemmas: ["the", "door"]);

		var extractor = new IntentSeedExtractor();

		// ACT
		var seed = extractor.Extract(parsed);

		RenderSeed("the door", parsed, seed);

		// ASSERT
		Assert.Null(seed.Verb);
		Assert.Contains("door", seed.Objects);
	}

	private static void RenderSeed(
		string input,
		ParsedInput parsed,
		IntentSeed seed)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title($"[bold yellow]Intent Seed[/] for \"{input}\"")
			.AddColumn("Field")
			.AddColumn("Value");

		table.AddRow("Raw Text", parsed.RawText);
		table.AddRow("Normalized", parsed.NormalizedText);
		table.AddRow("Tokens", string.Join(", ", parsed.Tokens));
		table.AddRow("Lemmas", string.Join(", ", parsed.Lemmas));
		table.AddRow("Verb", seed.Verb ?? "<none>");
		table.AddRow(
			"Objects",
			seed.Objects.Count > 0
				? string.Join(", ", seed.Objects)
				: "<none>");

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}
}
