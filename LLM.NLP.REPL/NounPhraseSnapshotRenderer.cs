namespace LLM.NLP.REPL;

using Spectre.Console;

/// <summary>
/// Renders a structured snapshot of a <see cref="NounPhrase"/>.
/// </summary>
public static class NounPhraseSnapshotRenderer
{
	public static void Render(
		string title,
		NounPhrase phrase)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title(title)
			.AddColumn("Field")
			.AddColumn("Value");

		table.AddRow("Head", $"[green]{phrase.Head}[/]");

		table.AddRow(
			"Modifiers",
			phrase.Modifiers.Count > 0
				? string.Join(", ", phrase.Modifiers)
				: "[dim]<none>[/]");

		table.AddRow(
			"Text",
			$"[dim]\"{phrase.Text}\"[/]");

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();

		if (phrase.Complements.Count == 0)
		{
			AnsiConsole.MarkupLine("[grey]No complements[/]");
			AnsiConsole.WriteLine();
			return;
		}

		var compTable = new Table()
			.Border(TableBorder.Simple)
			.Title("Complements")
			.AddColumn("Prep")
			.AddColumn("Head");

		foreach (var (prep, complement) in phrase.Complements)
		{
			compTable.AddRow(
				$"[blue]{prep}[/]",
				$"[green]{complement.Head}[/]");
		}

		AnsiConsole.Write(compTable);
		AnsiConsole.WriteLine();

		// Recursive detail
		foreach (var (prep, complement) in phrase.Complements)
		{
			Render($"Complement ({prep})", complement);
		}
	}

	public static void RenderAll(
		string input,
		IReadOnlyList<NounPhrase> phrases)
	{
		if (phrases.Count == 0)
		{
			AnsiConsole.MarkupLine(
				$"[grey]No noun phrases extracted for \"{input}\"[/]");
			AnsiConsole.WriteLine();
			return;
		}

		for (var i = 0; i < phrases.Count; i++)
		{
			var title = phrases.Count == 1
				? $"Noun Phrase — \"{input}\""
				: $"Noun Phrase #{i + 1} — \"{input}\"";

			Render(title, phrases[i]);
		}
	}

	public static void RenderCompact(
		NounPhrase phrase,
		int indent = 0)
	{
		var pad = new string(' ', indent * 2);

		AnsiConsole.MarkupLine(
			$"{pad}[green]NP:[/] {phrase.Head} " +
			$"[dim](\"{phrase.Text}\")[/]");

		if (phrase.Modifiers.Count > 0)
		{
			AnsiConsole.MarkupLine(
				$"{pad}  [yellow]mods:[/] " +
				string.Join(", ", phrase.Modifiers));
		}

		foreach (var (prep, complement) in phrase.Complements)
		{
			AnsiConsole.MarkupLine(
				$"{pad}  [blue]{prep} →[/] {complement.Head}");

			RenderCompact(complement, indent + 2);
		}
	}
}
