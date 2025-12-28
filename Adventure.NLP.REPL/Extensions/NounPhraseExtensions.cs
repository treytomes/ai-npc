using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL;

public static class NounPhraseExtensions
{
	public static IRenderable ToRenderable(this NounPhrase phrase, string? title = null)
	{
		ArgumentNullException.ThrowIfNull(phrase);

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Field")
			.AddColumn("Value");

		if (!string.IsNullOrWhiteSpace(title))
		{
			table.Title(title);
		}

		// Head word
		table.AddRow(
			new Text("Head"),
			new Markup(RenderingColors.FormatColor(RenderingColors.Head, phrase.Head.EscapeMarkup()))
		);

		// Modifiers
		var modifiersMarkup = phrase.Modifiers.Count > 0
			? new Markup(string.Join(", ", phrase.Modifiers.Select(m => m.EscapeMarkup())))
			: new Markup(RenderingColors.FormatNone());
		table.AddRow(
			new Text("Modifiers"),
			modifiersMarkup
		);

		// Full text
		table.AddRow(
			new Text("Text"),
			new Markup(RenderingColors.FormatDim($"\"{phrase.Text.EscapeMarkup()}\""))
		);

		// If no complements, return just the main table
		if (phrase.Complements.Count == 0)
		{
			return table;
		}

		// Create a layout to hold both the main table and complements
		var layout = new Rows(
			table,
			new Text(string.Empty), // Empty line
			CreateComplementsRenderable(phrase.Complements)
		);

		return layout;
	}

	private static IRenderable CreateComplementsRenderable(
		IReadOnlyDictionary<string, NounPhrase> complements)
	{
		var complementsTable = new Table()
			.Border(TableBorder.Simple)
			.Title("Complements")
			.AddColumn("Prep")
			.AddColumn("Head");

		foreach (var (prep, complement) in complements)
		{
			complementsTable.AddRow(
				new Markup(RenderingColors.FormatColor(RenderingColors.Preposition, prep.EscapeMarkup())),
				new Markup(RenderingColors.FormatColor(RenderingColors.Head, complement.Head.EscapeMarkup()))
			);
		}

		// Create a list of renderables for nested complements
		var renderables = new List<IRenderable> { complementsTable };

		foreach (var (prep, complement) in complements)
		{
			renderables.Add(new Text(string.Empty)); // Empty line
			renderables.Add(complement.ToRenderable($"Complement ({prep})"));
		}

		return new Rows(renderables);
	}

	/// <summary>
	/// Creates a compact tree-style renderable representation of the noun phrase.
	/// </summary>
	public static IRenderable ToCompactRenderable(this NounPhrase phrase, int indent = 0)
	{
		ArgumentNullException.ThrowIfNull(phrase);

		var tree = new Tree(
			$"{RenderingColors.FormatColor(RenderingColors.Head, phrase.Head.EscapeMarkup())} " +
			$"{RenderingColors.FormatDim($"(\"{phrase.Text.EscapeMarkup()}\")")}"
		);
		tree.Style = Style.Plain;

		if (phrase.Modifiers.Count > 0)
		{
			var modifiersNode = tree.AddNode($"[{RenderingColors.Modifier}]modifiers:[/]");
			foreach (var modifier in phrase.Modifiers)
			{
				modifiersNode.AddNode(modifier.EscapeMarkup());
			}
		}

		if (phrase.Complements.Count > 0)
		{
			var complementsNode = tree.AddNode($"[{RenderingColors.Complement}]complements:[/]");
			foreach (var (prep, complement) in phrase.Complements)
			{
				var prepNode = complementsNode.AddNode(
					$"{RenderingColors.FormatColor(RenderingColors.Preposition, prep.EscapeMarkup())} →"
				);
				AddComplementToNode(prepNode, complement);
			}
		}

		return tree;
	}

	private static void AddComplementToNode(TreeNode parent, NounPhrase phrase)
	{
		var node = parent.AddNode(
			$"{RenderingColors.FormatColor(RenderingColors.Head, phrase.Head.EscapeMarkup())} " +
			$"{RenderingColors.FormatDim($"(\"{phrase.Text.EscapeMarkup()}\")")}"
		);

		if (phrase.Modifiers.Count > 0)
		{
			var modifiersNode = node.AddNode($"[{RenderingColors.Modifier}]modifiers:[/]");
			foreach (var modifier in phrase.Modifiers)
			{
				modifiersNode.AddNode(modifier.EscapeMarkup());
			}
		}

		if (phrase.Complements.Count > 0)
		{
			var complementsNode = node.AddNode($"[{RenderingColors.Complement}]complements:[/]");
			foreach (var (prep, complement) in phrase.Complements)
			{
				var prepNode = complementsNode.AddNode(
					$"{RenderingColors.FormatColor(RenderingColors.Preposition, prep.EscapeMarkup())} →"
				);
				AddComplementToNode(prepNode, complement);
			}
		}
	}
}