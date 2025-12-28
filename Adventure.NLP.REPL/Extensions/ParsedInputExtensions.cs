using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL;

/// <summary>
/// Extension methods for rendering <see cref="ParsedInput"/> objects using Spectre.Console.
/// </summary>
public static class ParsedInputExtensions
{
	/// <summary>
	/// Creates a snapshot-style renderable view of the parsed input.
	/// </summary>
	/// <param name="parsed">The parsed input to render.</param>
	/// <param name="rawInput">The original raw input string.</param>
	/// <param name="title">Optional title for the panel.</param>
	/// <returns>An IRenderable representation of the parsed input.</returns>
	public static IRenderable ToSnapshotRenderable(this ParsedInput parsed, string? rawInput = null, string? title = null)
	{
		ArgumentNullException.ThrowIfNull(parsed);

		var grid = new Grid()
			.AddColumn(new GridColumn().NoWrap())
			.AddColumn();

		// Add raw input if provided
		if (rawInput != null)
		{
			grid.AddRow(
				$"[{RenderingColors.Bold}]Input[/]",
				RenderingColors.FormatColor(RenderingColors.Verb, $"\"{rawInput.EscapeMarkup()}\""));
		}

		grid.AddRow(
			$"[{RenderingColors.Bold}]Raw Text[/]",
			parsed.RawText?.EscapeMarkup() ?? RenderingColors.FormatNone());

		grid.AddRow(
			$"[{RenderingColors.Bold}]Normalized[/]",
			parsed.NormalizedText?.EscapeMarkup() ?? RenderingColors.FormatNone());

		grid.AddRow(
			$"[{RenderingColors.Bold}]Tokens[/]",
			parsed.Tokens.Count > 0
				? string.Join(" · ", parsed.Tokens.Select(t => t.EscapeMarkup()))
				: RenderingColors.FormatNone());

		grid.AddRow(
			$"[{RenderingColors.Bold}]Lemmas[/]",
			parsed.Lemmas.Count > 0
				? string.Join(" · ", parsed.Lemmas.Select(l => l.EscapeMarkup()))
				: RenderingColors.FormatNone());

		var panel = new Panel(grid)
			.Header(title ?? $"[{RenderingColors.Bold} {RenderingColors.Subject}]Parsed Input Snapshot[/]")
			.Border(BoxBorder.Rounded);

		// If no tokens, return just the panel
		if (parsed.ParsedTokens.Count == 0)
		{
			return panel;
		}

		// Create a layout with both panel and token table
		var renderables = new List<IRenderable> { panel };

		var tokenTable = CreateTokenTable(parsed);
		if (tokenTable != null)
		{
			renderables.Add(new Text(string.Empty)); // Empty line
			renderables.Add(tokenTable);
		}

		return new Rows(renderables);
	}

	/// <summary>
	/// Creates a detailed table view of the parsed tokens.
	/// </summary>
	public static IRenderable ToTokenTableRenderable(this ParsedInput parsed, string? title = null)
	{
		ArgumentNullException.ThrowIfNull(parsed);

		return CreateTokenTable(parsed, title) ?? (IRenderable)new Markup(RenderingColors.FormatNone());
	}

	/// <summary>
	/// Creates a compact single-line representation of the parsed input.
	/// </summary>
	public static IRenderable ToCompactRenderable(this ParsedInput parsed)
	{
		ArgumentNullException.ThrowIfNull(parsed);

		if (parsed.Tokens.Count == 0)
		{
			return new Markup(RenderingColors.FormatNone());
		}

		var parts = new List<string>();
		for (var i = 0; i < parsed.ParsedTokens.Count; i++)
		{
			var token = parsed.ParsedTokens[i];
			var color = GetPosColor(token.Pos);
			parts.Add(RenderingColors.FormatColor(color, token.Value.EscapeMarkup()));
		}

		return new Markup(string.Join(" ", parts));
	}

	private static Table? CreateTokenTable(ParsedInput parsed, string? title = null)
	{
		if (parsed.ParsedTokens.Count == 0)
			return null;

		var table = new Table()
			.Border(TableBorder.Simple)
			.Title(title ?? $"[{RenderingColors.Bold}]Token Details[/]")
			.AddColumn("#")
			.AddColumn("Value")
			.AddColumn("Lemma")
			.AddColumn("POS");

		for (var i = 0; i < parsed.ParsedTokens.Count; i++)
		{
			var token = parsed.ParsedTokens[i];
			var posColor = GetPosColor(token.Pos);

			table.AddRow(
				i.ToString(),
				token.Value.EscapeMarkup(),
				token.Lemma.EscapeMarkup(),
				RenderingColors.FormatColor(posColor, token.Pos.ToString()));
		}

		return table;
	}

	private static string GetPosColor(NlpPartOfSpeech pos)
	{
		return pos switch
		{
			NlpPartOfSpeech.Noun => RenderingColors.Head,
			NlpPartOfSpeech.Verb => RenderingColors.Verb,
			NlpPartOfSpeech.Adjective => RenderingColors.Modifier,
			NlpPartOfSpeech.Adverb => RenderingColors.Modifier,
			NlpPartOfSpeech.Pronoun => RenderingColors.Subject,
			NlpPartOfSpeech.Determiner => RenderingColors.Dim,
			_ => RenderingColors.None
		};
	}
}