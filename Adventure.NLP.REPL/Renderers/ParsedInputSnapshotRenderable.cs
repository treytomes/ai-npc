using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class ParsedInputSnapshotRenderable : Common.Renderables.Renderable
{
	private readonly ParsedInput _parsed;
	private readonly string? _rawInput;
	private readonly string? _title;

	public ParsedInputSnapshotRenderable(ParsedInput parsed, string? rawInput = null, string? title = null)
	{
		_parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
		_rawInput = rawInput;
		_title = title;
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var grid = new Grid()
			.AddColumn(new GridColumn().NoWrap())
			.AddColumn();

		if (_rawInput != null)
		{
			grid.AddRow(
				$"[{RenderingColors.UI.Bold}]Input[/]",
				RenderingColors.FormatColor(RenderingColors.Grammar.Verb, $"\"{_rawInput.EscapeMarkup()}\""));
		}

		grid.AddRow(
			$"[{RenderingColors.UI.Bold}]Raw Text[/]",
			_parsed.RawText?.EscapeMarkup() ?? RenderingColors.FormatNone());

		grid.AddRow(
			$"[{RenderingColors.UI.Bold}]Normalized[/]",
			_parsed.NormalizedText?.EscapeMarkup() ?? RenderingColors.FormatNone());

		grid.AddRow(
			$"[{RenderingColors.UI.Bold}]Tokens[/]",
			_parsed.Tokens.Count > 0
				? string.Join(" · ", _parsed.Tokens.Select(t => t.EscapeMarkup()))
				: RenderingColors.FormatNone());

		grid.AddRow(
			$"[{RenderingColors.UI.Bold}]Lemmas[/]",
			_parsed.Lemmas.Count > 0
				? string.Join(" · ", _parsed.Lemmas.Select(l => l.EscapeMarkup()))
				: RenderingColors.FormatNone());

		var panel = new Panel(grid)
			.Header(_title ?? $"[{RenderingColors.UI.Bold} {RenderingColors.Grammar.Subject}]Parsed Input Snapshot[/]")
			.Border(BoxBorder.Rounded);

		yield return panel;

		if (_parsed.ParsedTokens.Count > 0)
		{
			yield return RenderingColors.EmptyLine();
			yield return new TokenTableRenderable(_parsed);
		}
	}
}