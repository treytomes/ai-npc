using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class IntentSeedRenderable : Common.Renderables.Renderable
{
	private readonly IntentSeed _seed;
	private readonly string? _title;
	private readonly bool _detailed;

	public IntentSeedRenderable(IntentSeed seed, string? title = null, bool detailed = false)
	{
		_seed = seed ?? throw new ArgumentNullException(nameof(seed));
		_title = title;
		_detailed = detailed;
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return CreateSummaryTable();

		if (_detailed)
		{
			if (_seed.Subject != null)
			{
				yield return RenderingColors.EmptyLine();
				yield return new NounPhraseRenderable(_seed.Subject, "Subject Details");
			}

			if (_seed.IndirectObject != null)
			{
				yield return RenderingColors.EmptyLine();
				yield return new NounPhraseRenderable(_seed.IndirectObject, "Indirect Object Details");
			}

			if (_seed.DirectObject != null)
			{
				yield return RenderingColors.EmptyLine();
				yield return new NounPhraseRenderable(_seed.DirectObject, "Direct Object Details");
			}

			foreach (var (prep, phrase) in _seed.Prepositions)
			{
				yield return RenderingColors.EmptyLine();
				yield return new NounPhraseRenderable(phrase, $"Prepositional Phrase ({prep})");
			}
		}
	}

	private Table CreateSummaryTable()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Field")
			.AddColumn("Value");

		if (!string.IsNullOrWhiteSpace(_title))
		{
			table.Title(_title);
		}

		table.AddRow(
			new Text("Subject"),
			CreateNounPhraseMarkup(_seed.Subject, RenderingColors.Grammar.Subject)
		);

		table.AddRow(
			new Text("Verb"),
			_seed.Verb != null
				? new Markup(RenderingColors.FormatColor(RenderingColors.Grammar.Verb, _seed.Verb.EscapeMarkup()))
				: new Markup(RenderingColors.FormatNone())
		);

		table.AddRow(
			new Text("Indirect Object"),
			CreateNounPhraseMarkup(_seed.IndirectObject, RenderingColors.Grammar.IndirectObject)
		);

		table.AddRow(
			new Text("Direct Object"),
			CreateNounPhraseMarkup(_seed.DirectObject, RenderingColors.Grammar.DirectObject)
		);

		var prepositionsMarkup = _seed.Prepositions.Count == 0
			? new Markup(RenderingColors.FormatNone())
			: new Markup(string.Join(", ", _seed.Prepositions.Select(p =>
				$"{RenderingColors.FormatColor(RenderingColors.Grammar.Preposition, p.Key.EscapeMarkup())} → {p.Value.Head.EscapeMarkup()}")));
		table.AddRow(
			new Text("Prepositions"),
			prepositionsMarkup
		);

		return table;
	}

	private static IRenderable CreateNounPhraseMarkup(NounPhrase? phrase, string color)
	{
		if (phrase == null)
		{
			return new Markup(RenderingColors.FormatNone());
		}

		return new Markup(
			$"{RenderingColors.FormatColor(color, phrase.Head.EscapeMarkup())} " +
			$"{RenderingColors.FormatDim($"(\"{phrase.Text.EscapeMarkup()}\")")}"
		);
	}
}