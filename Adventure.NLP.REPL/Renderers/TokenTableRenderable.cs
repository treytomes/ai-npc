using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class TokenTableRenderable : Common.Renderables.Renderable
{
	private readonly ParsedInput _parsed;
	private readonly string? _title;

	public TokenTableRenderable(ParsedInput parsed, string? title = null)
	{
		_parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
		_title = title;
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		if (_parsed.ParsedTokens.Count == 0)
		{
			yield return new Markup(RenderingColors.FormatNone());
			yield break;
		}

		var table = new Table()
			.Border(TableBorder.Simple)
			.Title(_title ?? $"[{RenderingColors.UI.Bold}]Token Details[/]")
			.AddColumn("#")
			.AddColumn("Value")
			.AddColumn("Lemma")
			.AddColumn("POS");

		for (var i = 0; i < _parsed.ParsedTokens.Count; i++)
		{
			var token = _parsed.ParsedTokens[i];
			var posColor = RenderingColors.GetPosColor(token.Pos);

			table.AddRow(
				i.ToString(),
				token.Value.EscapeMarkup(),
				token.Lemma.EscapeMarkup(),
				RenderingColors.FormatColor(posColor, token.Pos.ToString()));
		}

		yield return table;
	}
}