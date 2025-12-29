using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class ComplementsRenderable : Adventure.Renderables.Renderable
{
	private readonly IReadOnlyDictionary<string, NounPhrase> _complements;

	public ComplementsRenderable(IReadOnlyDictionary<string, NounPhrase> complements)
	{
		_complements = complements ?? throw new ArgumentNullException(nameof(complements));
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var complementsTable = new Table()
			.Border(TableBorder.Simple)
			.Title("Complements")
			.AddColumn("Prep")
			.AddColumn("Head");

		foreach (var (prep, complement) in _complements)
		{
			complementsTable.AddRow(
				new Markup(RenderingColors.FormatColor(RenderingColors.Grammar.Preposition, prep.EscapeMarkup())),
				new Markup(RenderingColors.FormatColor(RenderingColors.NounPhrase.Head, complement.Head.EscapeMarkup()))
			);
		}

		yield return complementsTable;

		foreach (var (prep, complement) in _complements)
		{
			yield return RenderingColors.EmptyLine();
			yield return new NounPhraseRenderable(complement, $"Complement ({prep})");
		}
	}
}