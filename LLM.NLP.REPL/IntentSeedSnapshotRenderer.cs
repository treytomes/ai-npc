using Spectre.Console;

namespace LLM.NLP.REPL;

/// <summary>
/// Renders a snapshot-style visualization of an <see cref="IntentSeed"/>
/// for debugging and regression inspection.
/// </summary>
public static class IntentSeedSnapshotRenderer
{
	public static void Render(
		string input,
		ParsedInput parsed,
		IntentSeed seed)
	{
		AnsiConsole.Write(new Rule($"[bold yellow]Intent Snapshot[/] — \"{input}\""));

		// Parsed tokens
		var tokenTable = new Table()
			.Border(TableBorder.Simple)
			.Title("Parsed Tokens")
			.AddColumn("Value")
			.AddColumn("Lemma")
			.AddColumn("POS");

		foreach (var token in parsed.ParsedTokens)
		{
			tokenTable.AddRow(
				token.Value,
				token.Lemma,
				token.Pos.ToString());
		}

		AnsiConsole.Write(tokenTable);
		AnsiConsole.WriteLine();

		// Intent seed
		var intentTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("Intent Seed")
			.AddColumn("Field")
			.AddColumn("Value");

		intentTable.AddRow("Verb", seed.Verb ?? "<none>");
		intentTable.AddRow("Direct Object", seed.DirectObject ?? "<none>");

		intentTable.AddRow(
			"Prepositions",
			seed.Prepositions.Count == 0
				? "<none>"
				: string.Join(
					"\n",
					seed.Prepositions.Select(kvp =>
						$"{kvp.Key} → {kvp.Value}")));

		AnsiConsole.Write(intentTable);
		AnsiConsole.WriteLine();
	}
}
