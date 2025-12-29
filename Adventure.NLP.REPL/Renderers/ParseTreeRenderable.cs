using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class ParseTreeRenderable : Common.Renderables.Renderable
{
	private readonly ParsedInput _parsed;
	private readonly string _input;
	private readonly IntentSeed? _seed;

	public ParseTreeRenderable(ParsedInput parsed, string input, IntentSeed? seed = null)
	{
		_parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
		_input = input ?? throw new ArgumentNullException(nameof(input));
		_seed = seed;
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new Rule(
			$"[{RenderingColors.UI.Bold} {RenderingColors.Grammar.Verb}]Parse Tree[/] — \"{_input.EscapeMarkup()}\"")
			.LeftJustified();

		var root = new Tree($"[{RenderingColors.UI.Bold}]Sentence[/]");
		root.Style = Style.Plain;

		AddTokensToTree(root, _parsed);

		if (_seed != null)
		{
			AddIntentSeedToTree(root, _seed);
		}

		yield return root;
	}

	private static void AddTokensToTree(Tree root, ParsedInput parsed)
	{
		var tokenNode = root.AddNode($"[{RenderingColors.Grammar.Preposition}]Tokens[/]");

		foreach (var token in parsed.ParsedTokens)
		{
			var posColor = RenderingColors.GetPosColor(token.Pos);
			tokenNode.AddNode(
				$"{token.Value.EscapeMarkup()} " +
				RenderingColors.FormatDim($"(lemma: {token.Lemma.EscapeMarkup()}, POS: [{posColor}]{token.Pos}[/])"));
		}
	}

	private static void AddIntentSeedToTree(Tree root, IntentSeed seed)
	{
		var intentNode = root.AddNode($"[{RenderingColors.Grammar.Verb}]Intent Seed[/]");

		// Subject
		if (seed.Subject != null)
		{
			var subjNode = intentNode.AddNode($"[{RenderingColors.Grammar.Subject}]Subject[/]");
			RenderNounPhraseTree(subjNode, seed.Subject, 0);
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.Grammar.Subject}]Subject[/]: {RenderingColors.NoneText}");
		}

		// Verb
		intentNode.AddNode(
			$"[{RenderingColors.Grammar.Verb}]Verb[/]: " +
			(seed.Verb != null
				? $"[{RenderingColors.UI.Bold}]{seed.Verb.EscapeMarkup()}[/]"
				: RenderingColors.NoneText));

		// Indirect Object
		if (seed.IndirectObject != null)
		{
			var ioNode = intentNode.AddNode($"[{RenderingColors.Grammar.IndirectObject}]Indirect Object[/]");
			RenderNounPhraseTree(ioNode, seed.IndirectObject, 0);
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.Grammar.IndirectObject}]Indirect Object[/]: {RenderingColors.NoneText}");
		}

		// Direct Object
		if (seed.DirectObject != null)
		{
			var objNode = intentNode.AddNode($"[{RenderingColors.Grammar.DirectObject}]Direct Object[/]");
			RenderNounPhraseTree(objNode, seed.DirectObject, 0);
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.Grammar.DirectObject}]Direct Object[/]: {RenderingColors.NoneText}");
		}

		// Prepositions
		if (seed.Prepositions.Count > 0)
		{
			var prepNode = intentNode.AddNode($"[{RenderingColors.Grammar.Preposition}]Prepositions[/]");

			foreach (var (prep, phrase) in seed.Prepositions)
			{
				var pNode = prepNode.AddNode($"[italic]{prep.EscapeMarkup()}[/]");
				RenderNounPhraseTree(pNode, phrase, 0);
			}
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.Grammar.Preposition}]Prepositions[/]: {RenderingColors.NoneText}");
		}
	}

	private static void RenderNounPhraseTree(TreeNode parent, NounPhrase phrase, int depth)
	{
		const int MaxRecursionDepth = 10;

		if (depth >= MaxRecursionDepth)
		{
			parent.AddNode($"[{RenderingColors.UI.Dim}]... (max depth reached)[/]");
			return;
		}

		parent.AddNode($"Text: {RenderingColors.FormatDim(phrase.Text.EscapeMarkup())}");
		parent.AddNode($"Head: [{RenderingColors.UI.Bold}]{phrase.Head.EscapeMarkup()}[/]");

		if (phrase.Modifiers.Count > 0)
		{
			var modNode = parent.AddNode("Modifiers");
			foreach (var mod in phrase.Modifiers)
			{
				modNode.AddNode(mod.EscapeMarkup());
			}
		}
		else
		{
			parent.AddNode($"Modifiers: {RenderingColors.NoneText}");
		}

		if (phrase.Complements.Count > 0)
		{
			var compNode = parent.AddNode("Complements");
			foreach (var (prep, np) in phrase.Complements)
			{
				var pNode = compNode.AddNode(prep.EscapeMarkup());
				RenderNounPhraseTree(pNode, np, depth + 1);
			}
		}
		else
		{
			parent.AddNode($"Complements: {RenderingColors.NoneText}");
		}
	}
}