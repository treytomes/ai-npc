using Spectre.Console;

namespace Adventure.NLP.REPL.Renderers;

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
		AnsiConsole.Write(
			new Rule($"[bold yellow]Intent Snapshot[/] — \"{input}\"")
				.LeftJustified());

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

		// Intent summary
		var intentTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("Intent Seed")
			.AddColumn("Field")
			.AddColumn("Value");

		intentTable.AddRow(
			"Subject",
			seed.Subject != null
				? $"[cyan]{seed.Subject.Head}[/] " +
				  $"[dim](\"{seed.Subject.Text}\")[/]"
				: "<none>");

		intentTable.AddRow(
			"Verb",
			seed.Verb != null
				? $"[yellow]{seed.Verb}[/]"
				: "<none>");

		intentTable.AddRow(
			"Indirect Object",
			seed.IndirectObject != null
				? $"[magenta]{seed.IndirectObject.Head}[/] " +
				  $"[dim](\"{seed.IndirectObject.Text}\")[/]"
				: "<none>");

		intentTable.AddRow(
			"Direct Object",
			seed.DirectObject != null
				? $"[green]{seed.DirectObject.Head}[/] " +
				  $"[dim](\"{seed.DirectObject.Text}\")[/]"
				: "<none>");

		intentTable.AddRow(
			"Prepositions",
			seed.Prepositions.Count == 0
				? "<none>"
				: string.Join(
					", ",
					seed.Prepositions.Select(p =>
						$"[blue]{p.Key}[/] → {p.Value.Head}")));

		AnsiConsole.Write(intentTable);
		AnsiConsole.WriteLine();

		// Detailed breakdown
		if (seed.Subject != null)
		{
			NounPhraseSnapshotRenderer.Render(
				"Subject",
				seed.Subject);
		}

		if (seed.IndirectObject != null)
		{
			NounPhraseSnapshotRenderer.Render(
				"Indirect Object",
				seed.IndirectObject);
		}

		if (seed.DirectObject != null)
		{
			NounPhraseSnapshotRenderer.Render(
				"Direct Object",
				seed.DirectObject);
		}

		foreach (var (prep, phrase) in seed.Prepositions)
		{
			NounPhraseSnapshotRenderer.Render(
				$"Preposition ({prep})",
				phrase);
		}
	}

	public static void RenderCompact(IntentSeed seed)
	{
		AnsiConsole.MarkupLine($"[bold]Verb:[/] {seed.Verb ?? "<none>"}");

		if (seed.Subject != null)
		{
			AnsiConsole.MarkupLine(
				$"  [cyan]Subj:[/] {seed.Subject.Head} " +
				$"[dim](\"{seed.Subject.Text}\")[/]");
		}

		if (seed.IndirectObject != null)
		{
			AnsiConsole.MarkupLine(
				$"  [magenta]IO:[/] {seed.IndirectObject.Head} " +
				$"[dim](\"{seed.IndirectObject.Text}\")[/]");
		}

		if (seed.DirectObject != null)
		{
			AnsiConsole.MarkupLine(
				$"  [green]DO:[/] {seed.DirectObject.Head} " +
				$"[dim](\"{seed.DirectObject.Text}\")[/]");
		}

		foreach (var (prep, phrase) in seed.Prepositions)
		{
			AnsiConsole.MarkupLine(
				$"  [blue]{prep}:[/] {phrase.Head}");
		}

		AnsiConsole.WriteLine();
	}
}