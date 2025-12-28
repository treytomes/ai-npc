using Spectre.Console;

namespace Adventure.NLP.REPL.Renderers;

/// <summary>
/// Renders a snapshot-style view of <see cref="ParsedInput"/> using Spectre.Console.
/// Intended for test diagnostics and visual inspection, not assertions.
/// </summary>
public static class ParsedInputSnapshotRenderer
{
	/// <summary>
	/// Render a parsed input snapshot for the given raw input.
	/// </summary>
	public static void Render(string input, ParsedInput parsed)
	{
		var grid = new Grid()
			.AddColumn(new GridColumn().NoWrap())
			.AddColumn();

		grid.AddRow(
			"[bold]Input[/]",
			$"[yellow]\"{input}\"[/]");

		grid.AddRow(
			"[bold]Raw Text[/]",
			parsed.RawText ?? "<null>");

		grid.AddRow(
			"[bold]Normalized[/]",
			parsed.NormalizedText ?? "<null>");

		grid.AddRow(
			"[bold]Tokens[/]",
			parsed.Tokens.Count > 0
				? string.Join(" · ", parsed.Tokens)
				: "<none>");

		grid.AddRow(
			"[bold]Lemmas[/]",
			parsed.Lemmas.Count > 0
				? string.Join(" · ", parsed.Lemmas)
				: "<none>");

		AnsiConsole.Write(
			new Panel(grid)
				.Header("[bold cyan]Parsed Input Snapshot[/]")
				.Border(BoxBorder.Rounded));

		RenderTokenTable(parsed);

		AnsiConsole.WriteLine();
	}

	private static void RenderTokenTable(ParsedInput parsed)
	{
		if (parsed.ParsedTokens.Count == 0)
			return;

		var table = new Table()
			.Border(TableBorder.Simple)
			.Title("[bold]Token Details[/]")
			.AddColumn("#")
			.AddColumn("Value")
			.AddColumn("Lemma")
			.AddColumn("POS");

		for (var i = 0; i < parsed.ParsedTokens.Count; i++)
		{
			var token = parsed.ParsedTokens[i];

			table.AddRow(
				i.ToString(),
				token.Value,
				token.Lemma,
				token.Pos.ToString());
		}

		AnsiConsole.Write(table);
	}
}
