using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class CompactIntentSeedRenderable : Adventure.Renderables.Renderable
{
	private readonly IntentSeed _seed;

	public CompactIntentSeedRenderable(IntentSeed seed)
	{
		_seed = seed ?? throw new ArgumentNullException(nameof(seed));
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var tree = new Tree(_seed.Verb != null
			? RenderingColors.FormatColor(RenderingColors.Grammar.Verb, _seed.Verb.EscapeMarkup())
			: RenderingColors.FormatNoVerb());
		tree.Style = Style.Plain;

		if (_seed.Subject != null)
		{
			var subjectNode = tree.AddNode($"[{RenderingColors.Grammar.Subject}]Subject:[/]");
			AddNounPhraseToNode(subjectNode, _seed.Subject, 0);
		}

		if (_seed.IndirectObject != null)
		{
			var ioNode = tree.AddNode($"[{RenderingColors.Grammar.IndirectObject}]Indirect Object:[/]");
			AddNounPhraseToNode(ioNode, _seed.IndirectObject, 0);
		}

		if (_seed.DirectObject != null)
		{
			var doNode = tree.AddNode($"[{RenderingColors.Grammar.DirectObject}]Direct Object:[/]");
			AddNounPhraseToNode(doNode, _seed.DirectObject, 0);
		}

		if (_seed.Prepositions.Count > 0)
		{
			var prepNode = tree.AddNode("Prepositional Phrases:");
			foreach (var (prep, phrase) in _seed.Prepositions)
			{
				var specificPrepNode = prepNode.AddNode(
					RenderingColors.FormatColor(RenderingColors.Grammar.Preposition, $"{prep.EscapeMarkup()}:")
				);
				AddNounPhraseToNode(specificPrepNode, phrase, 0);
			}
		}

		yield return tree;
	}

	private static void AddNounPhraseToNode(TreeNode parent, NounPhrase phrase, int depth)
	{
		const int MaxRecursionDepth = 10;

		if (depth >= MaxRecursionDepth)
		{
			parent.AddNode($"[{RenderingColors.UI.Dim}]... (max depth reached)[/]");
			return;
		}

		var node = parent.AddNode(
			$"{phrase.Head.EscapeMarkup()} {RenderingColors.FormatDim($"(\"{phrase.Text.EscapeMarkup()}\")")}"
		);

		if (phrase.Modifiers.Count > 0)
		{
			node.AddNode($"[{RenderingColors.NounPhrase.Modifier}]mods:[/] {string.Join(", ", phrase.Modifiers.Select(m => m.EscapeMarkup()))}");
		}

		if (phrase.Complements.Count > 0)
		{
			var compNode = node.AddNode("complements:");
			foreach (var (prep, complement) in phrase.Complements)
			{
				var prepNode = compNode.AddNode(
					$"{RenderingColors.FormatColor(RenderingColors.Grammar.Preposition, prep.EscapeMarkup())} →"
				);
				AddNounPhraseToNode(prepNode, complement, depth + 1);
			}
		}
	}
}