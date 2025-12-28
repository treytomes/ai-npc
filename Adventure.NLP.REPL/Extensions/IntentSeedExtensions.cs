using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL;

public static class IntentSeedExtensions
{
	public static IRenderable ToRenderable(this IntentSeed seed, string? title = null)
	{
		ArgumentNullException.ThrowIfNull(seed);

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Field")
			.AddColumn("Value");

		if (!string.IsNullOrWhiteSpace(title))
		{
			table.Title(title);
		}

		// Subject
		table.AddRow(
			new Text("Subject"),
			CreateNounPhraseMarkup(seed.Subject, RenderingColors.Subject)
		);

		// Verb
		table.AddRow(
			new Text("Verb"),
			seed.Verb != null
				? new Markup(RenderingColors.FormatColor(RenderingColors.Verb, seed.Verb.EscapeMarkup()))
				: new Markup(RenderingColors.FormatNone())
		);

		// Indirect Object
		table.AddRow(
			new Text("Indirect Object"),
			CreateNounPhraseMarkup(seed.IndirectObject, RenderingColors.IndirectObject)
		);

		// Direct Object
		table.AddRow(
			new Text("Direct Object"),
			CreateNounPhraseMarkup(seed.DirectObject, RenderingColors.DirectObject)
		);

		// Prepositions
		var prepositionsMarkup = seed.Prepositions.Count == 0
			? new Markup(RenderingColors.FormatNone())
			: new Markup(string.Join(", ", seed.Prepositions.Select(p =>
				$"{RenderingColors.FormatColor(RenderingColors.Preposition, p.Key.EscapeMarkup())} → {p.Value.Head.EscapeMarkup()}")));
		table.AddRow(
			new Text("Prepositions"),
			prepositionsMarkup
		);

		return table;
	}

	public static IRenderable ToDetailedRenderable(this IntentSeed seed, string? title = null)
	{
		ArgumentNullException.ThrowIfNull(seed);

		var renderables = new List<IRenderable>();

		// Add the main summary table
		renderables.Add(seed.ToRenderable(title));

		// Add detailed noun phrase breakdowns
		if (seed.Subject != null)
		{
			renderables.Add(new Text(string.Empty));
			renderables.Add(seed.Subject.ToRenderable("Subject Details"));
		}

		if (seed.IndirectObject != null)
		{
			renderables.Add(new Text(string.Empty));
			renderables.Add(seed.IndirectObject.ToRenderable("Indirect Object Details"));
		}

		if (seed.DirectObject != null)
		{
			renderables.Add(new Text(string.Empty));
			renderables.Add(seed.DirectObject.ToRenderable("Direct Object Details"));
		}

		foreach (var (prep, phrase) in seed.Prepositions)
		{
			renderables.Add(new Text(string.Empty));
			renderables.Add(phrase.ToRenderable($"Prepositional Phrase ({prep})"));
		}

		return new Rows(renderables);
	}

	public static IRenderable ToCompactRenderable(this IntentSeed seed)
	{
		ArgumentNullException.ThrowIfNull(seed);

		var tree = new Tree(seed.Verb != null
			? RenderingColors.FormatColor(RenderingColors.Verb, seed.Verb.EscapeMarkup())
			: RenderingColors.FormatNoVerb());
		tree.Style = Style.Plain;

		if (seed.Subject != null)
		{
			var subjectNode = tree.AddNode($"[{RenderingColors.Subject}]Subject:[/]");
			AddNounPhraseToNode(subjectNode, seed.Subject);
		}

		if (seed.IndirectObject != null)
		{
			var ioNode = tree.AddNode($"[{RenderingColors.IndirectObject}]Indirect Object:[/]");
			AddNounPhraseToNode(ioNode, seed.IndirectObject);
		}

		if (seed.DirectObject != null)
		{
			var doNode = tree.AddNode($"[{RenderingColors.DirectObject}]Direct Object:[/]");
			AddNounPhraseToNode(doNode, seed.DirectObject);
		}

		if (seed.Prepositions.Count > 0)
		{
			var prepNode = tree.AddNode("Prepositional Phrases:");
			foreach (var (prep, phrase) in seed.Prepositions)
			{
				var specificPrepNode = prepNode.AddNode(
					RenderingColors.FormatColor(RenderingColors.Preposition, $"{prep.EscapeMarkup()}:")
				);
				AddNounPhraseToNode(specificPrepNode, phrase);
			}
		}

		return tree;
	}

	/// <summary>
	/// Creates a full analysis view combining parsed tokens and intent structure.
	/// </summary>
	public static IRenderable ToAnalysisRenderable(
		this IntentSeed seed,
		string input,
		ParsedInput parsed)
	{
		ArgumentNullException.ThrowIfNull(seed);
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(parsed);

		var rows = new List<IRenderable>();

		// Header
		rows.Add(new Rule($"[{RenderingColors.Bold} {RenderingColors.Verb}]Intent Analysis[/] — \"{input.EscapeMarkup()}\"")
			.LeftJustified());
		rows.Add(new Text(string.Empty));

		// Parsed tokens table
		var tokenTable = new Table()
			.Border(TableBorder.Simple)
			.Title("Parsed Tokens")
			.AddColumn("Value")
			.AddColumn("Lemma")
			.AddColumn("POS");

		foreach (var token in parsed.ParsedTokens)
		{
			tokenTable.AddRow(
				token.Value.EscapeMarkup(),
				token.Lemma.EscapeMarkup(),
				token.Pos.ToString());
		}

		rows.Add(tokenTable);
		rows.Add(new Text(string.Empty));

		// Intent seed details
		rows.Add(seed.ToDetailedRenderable("Intent Seed"));

		var layout = new Rows(rows);
		return layout;
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

	private static void AddNounPhraseToNode(TreeNode parent, NounPhrase phrase)
	{
		var node = parent.AddNode(
			$"{phrase.Head.EscapeMarkup()} {RenderingColors.FormatDim($"(\"{phrase.Text.EscapeMarkup()}\")")}"
		);

		if (phrase.Modifiers.Count > 0)
		{
			node.AddNode($"[{RenderingColors.Modifier}]mods:[/] {string.Join(", ", phrase.Modifiers.Select(m => m.EscapeMarkup()))}");
		}

		if (phrase.Complements.Count > 0)
		{
			var compNode = node.AddNode("complements:");
			foreach (var (prep, complement) in phrase.Complements)
			{
				var prepNode = compNode.AddNode(
					$"{RenderingColors.FormatColor(RenderingColors.Preposition, prep.EscapeMarkup())} →"
				);
				AddNounPhraseToNode(prepNode, complement);
			}
		}
	}
}