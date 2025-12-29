using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class NounPhraseRenderable : Common.Renderables.Renderable
{
	private readonly NounPhrase _phrase;
	private readonly string? _title;
	private readonly bool _compact;
	private readonly int _maxDepth;

	public NounPhraseRenderable(NounPhrase phrase, string? title = null, bool compact = false, int maxDepth = 10)
	{
		_phrase = phrase ?? throw new ArgumentNullException(nameof(phrase));
		_title = title;
		_compact = compact;
		_maxDepth = maxDepth;
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		if (_compact)
		{
			yield return CreateCompactTree();
		}
		else
		{
			yield return CreateDetailedTable();

			if (_phrase.Complements.Count > 0)
			{
				yield return RenderingColors.EmptyLine();
				yield return new ComplementsRenderable(_phrase.Complements);
			}
		}
	}

	private Tree CreateCompactTree()
	{
		var tree = new Tree(
			$"{RenderingColors.FormatColor(RenderingColors.NounPhrase.Head, _phrase.Head.EscapeMarkup())} " +
			$"{RenderingColors.FormatDim($"(\"{_phrase.Text.EscapeMarkup()}\")")}"
		);
		tree.Style = Style.Plain;

		if (_phrase.Modifiers.Count > 0)
		{
			var modifiersNode = tree.AddNode($"[{RenderingColors.NounPhrase.Modifier}]modifiers:[/]");
			foreach (var modifier in _phrase.Modifiers)
			{
				modifiersNode.AddNode(modifier.EscapeMarkup());
			}
		}

		if (_phrase.Complements.Count > 0)
		{
			var complementsNode = tree.AddNode($"[{RenderingColors.NounPhrase.Complement}]complements:[/]");
			foreach (var (prep, complement) in _phrase.Complements)
			{
				var prepNode = complementsNode.AddNode(
					$"{RenderingColors.FormatColor(RenderingColors.Grammar.Preposition, prep.EscapeMarkup())} →"
				);
				AddComplementToNode(prepNode, complement, 0);
			}
		}

		return tree;
	}

	private Table CreateDetailedTable()
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
			new Text("Head"),
			new Markup(RenderingColors.FormatColor(RenderingColors.NounPhrase.Head, _phrase.Head.EscapeMarkup()))
		);

		var modifiersMarkup = _phrase.Modifiers.Count > 0
			? new Markup(string.Join(", ", _phrase.Modifiers.Select(m => m.EscapeMarkup())))
			: new Markup(RenderingColors.FormatNone());
		table.AddRow(
			new Text("Modifiers"),
			modifiersMarkup
		);

		table.AddRow(
			new Text("Text"),
			new Markup(RenderingColors.FormatDim($"\"{_phrase.Text.EscapeMarkup()}\""))
		);

		return table;
	}

	private void AddComplementToNode(TreeNode parent, NounPhrase phrase, int depth)
	{
		if (depth >= _maxDepth)
		{
			parent.AddNode($"[{RenderingColors.UI.Dim}]... (max depth reached)[/]");
			return;
		}

		var node = parent.AddNode(
			$"{RenderingColors.FormatColor(RenderingColors.NounPhrase.Head, phrase.Head.EscapeMarkup())} " +
			$"{RenderingColors.FormatDim($"(\"{phrase.Text.EscapeMarkup()}\")")}"
		);

		if (phrase.Modifiers.Count > 0)
		{
			var modifiersNode = node.AddNode($"[{RenderingColors.NounPhrase.Modifier}]modifiers:[/]");
			foreach (var modifier in phrase.Modifiers)
			{
				modifiersNode.AddNode(modifier.EscapeMarkup());
			}
		}

		if (phrase.Complements.Count > 0)
		{
			var complementsNode = node.AddNode($"[{RenderingColors.NounPhrase.Complement}]complements:[/]");
			foreach (var (prep, complement) in phrase.Complements)
			{
				var prepNode = complementsNode.AddNode(
					$"{RenderingColors.FormatColor(RenderingColors.Grammar.Preposition, prep.EscapeMarkup())} →"
				);
				AddComplementToNode(prepNode, complement, depth + 1);
			}
		}
	}
}