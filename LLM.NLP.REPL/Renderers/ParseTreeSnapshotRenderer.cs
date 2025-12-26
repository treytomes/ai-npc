using Spectre.Console;

namespace LLM.NLP.REPL.Renderers;

/// <summary>
/// Renders a full parse-tree snapshot using ParsedInput (lexical layer)
/// and IntentSeed (syntactic / semantic layer).
/// </summary>
public static class ParseTreeSnapshotRenderer
{
	public static void Render(
		string input,
		ParsedInput parsed,
		IntentSeed? seed = null)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule($"[bold yellow]Parse Tree[/] — \"{input}\"")
				.LeftJustified());

		var root = new Tree("[bold]Sentence[/]");

		/* ---------------- Tokens ---------------- */

		var tokenNode = root.AddNode("[blue]Tokens[/]");

		foreach (var token in parsed.ParsedTokens)
		{
			tokenNode.AddNode(
				$"[white]{token.Value}[/] " +
				$"[dim](lemma: {token.Lemma}, POS: {token.Pos})[/]");
		}

		/* ---------------- Intent Seed ---------------- */

		if (seed != null)
		{
			var intentNode = root.AddNode("[yellow]Intent Seed[/]");

			/* ---- Subject ---- */

			if (seed.Subject != null)
			{
				var subjNode = intentNode.AddNode("[cyan]Subject[/]");
				RenderNounPhraseTree(subjNode, seed.Subject);
			}
			else
			{
				intentNode.AddNode("[cyan]Subject[/]: <none>");
			}

			/* ---- Verb ---- */

			intentNode.AddNode(
				$"[yellow]Verb[/]: [bold]{seed.Verb ?? "<none>"}[/]");

			/* ---- Indirect Object ---- */

			if (seed.IndirectObject != null)
			{
				var ioNode = intentNode.AddNode("[magenta]Indirect Object[/]");
				RenderNounPhraseTree(ioNode, seed.IndirectObject);
			}
			else
			{
				intentNode.AddNode("[magenta]Indirect Object[/]: <none>");
			}

			/* ---- Direct Object ---- */

			if (seed.DirectObject != null)
			{
				var objNode = intentNode.AddNode("[green]Direct Object[/]");
				RenderNounPhraseTree(objNode, seed.DirectObject);
			}
			else
			{
				intentNode.AddNode("[green]Direct Object[/]: <none>");
			}

			/* ---- Prepositions ---- */

			if (seed.Prepositions.Count > 0)
			{
				var prepNode = intentNode.AddNode("[blue]Prepositions[/]");

				foreach (var (prep, phrase) in seed.Prepositions)
				{
					var pNode = prepNode.AddNode(
						$"[italic]{prep}[/]");
					RenderNounPhraseTree(pNode, phrase);
				}
			}
			else
			{
				intentNode.AddNode("[blue]Prepositions[/]: <none>");
			}
		}

		AnsiConsole.Write(root);
		AnsiConsole.WriteLine();
	}

	/* ---------------- Helpers ---------------- */

	private static void RenderNounPhraseTree(
		TreeNode parent,
		NounPhrase phrase)
	{
		parent.AddNode($"Text: [dim]{phrase.Text}[/]");
		parent.AddNode($"Head: [bold]{phrase.Head}[/]");

		if (phrase.Modifiers.Count > 0)
		{
			var modNode = parent.AddNode("Modifiers");
			foreach (var mod in phrase.Modifiers)
				modNode.AddNode(mod);
		}
		else
		{
			parent.AddNode("Modifiers: <none>");
		}

		if (phrase.Complements.Count > 0)
		{
			var compNode = parent.AddNode("Complements");
			foreach (var (prep, np) in phrase.Complements)
			{
				var pNode = compNode.AddNode(prep);
				RenderNounPhraseTree(pNode, np);
			}
		}
		else
		{
			parent.AddNode("Complements: <none>");
		}
	}
}