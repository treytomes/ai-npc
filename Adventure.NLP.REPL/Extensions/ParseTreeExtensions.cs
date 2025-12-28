using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL;

/// <summary>
/// Extension methods for rendering a full parse-tree view combining ParsedInput (lexical layer)
/// and IntentSeed (syntactic/semantic layer).
/// </summary>
public static class ParseTreeExtensions
{
	/// <summary>
	/// Creates a parse tree renderable combining lexical and syntactic information.
	/// </summary>
	/// <param name="parsed">The parsed input containing lexical information.</param>
	/// <param name="input">The original input string.</param>
	/// <param name="seed">Optional intent seed containing syntactic information.</param>
	/// <returns>An IRenderable representation of the parse tree.</returns>
	public static IRenderable ToParseTreeRenderable(
		this ParsedInput parsed,
		string input,
		IntentSeed? seed = null)
	{
		ArgumentNullException.ThrowIfNull(parsed);
		ArgumentNullException.ThrowIfNull(input);

		var renderables = new List<IRenderable>();

		// Header
		renderables.Add(new Rule(
			$"[{RenderingColors.Bold} {RenderingColors.Verb}]Parse Tree[/] — \"{input.EscapeMarkup()}\"")
			.LeftJustified());

		// Main tree
		var root = new Tree($"[{RenderingColors.Bold}]Sentence[/]");
		root.Style = Style.Plain;

		// Add tokens
		AddTokensToTree(root, parsed);

		// Add intent seed if available
		if (seed != null)
		{
			AddIntentSeedToTree(root, seed);
		}

		renderables.Add(root);

		return new Rows(renderables);
	}

	/// <summary>
	/// Creates a parse tree renderable from an IntentSeed and ParsedInput pair.
	/// </summary>
	public static IRenderable ToParseTreeRenderable(
		this IntentSeed seed,
		string input,
		ParsedInput parsed)
	{
		ArgumentNullException.ThrowIfNull(seed);
		return parsed.ToParseTreeRenderable(input, seed);
	}

	private static void AddTokensToTree(Tree root, ParsedInput parsed)
	{
		var tokenNode = root.AddNode($"[{RenderingColors.Preposition}]Tokens[/]");

		foreach (var token in parsed.ParsedTokens)
		{
			var posColor = GetPosColor(token.Pos);
			tokenNode.AddNode(
				$"{token.Value.EscapeMarkup()} " +
				RenderingColors.FormatDim($"(lemma: {token.Lemma.EscapeMarkup()}, POS: {token.Pos})"));
		}
	}

	private static void AddIntentSeedToTree(Tree root, IntentSeed seed)
	{
		var intentNode = root.AddNode($"[{RenderingColors.Verb}]Intent Seed[/]");

		// Subject
		if (seed.Subject != null)
		{
			var subjNode = intentNode.AddNode($"[{RenderingColors.Subject}]Subject[/]");
			RenderNounPhraseTree(subjNode, seed.Subject);
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.Subject}]Subject[/]: {RenderingColors.NoneText}");
		}

		// Verb
		intentNode.AddNode(
			$"[{RenderingColors.Verb}]Verb[/]: " +
			(seed.Verb != null
				? $"[{RenderingColors.Bold}]{seed.Verb.EscapeMarkup()}[/]"
				: RenderingColors.NoneText));

		// Indirect Object
		if (seed.IndirectObject != null)
		{
			var ioNode = intentNode.AddNode($"[{RenderingColors.IndirectObject}]Indirect Object[/]");
			RenderNounPhraseTree(ioNode, seed.IndirectObject);
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.IndirectObject}]Indirect Object[/]: {RenderingColors.NoneText}");
		}

		// Direct Object
		if (seed.DirectObject != null)
		{
			var objNode = intentNode.AddNode($"[{RenderingColors.DirectObject}]Direct Object[/]");
			RenderNounPhraseTree(objNode, seed.DirectObject);
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.DirectObject}]Direct Object[/]: {RenderingColors.NoneText}");
		}

		// Prepositions
		if (seed.Prepositions.Count > 0)
		{
			var prepNode = intentNode.AddNode($"[{RenderingColors.Preposition}]Prepositions[/]");

			foreach (var (prep, phrase) in seed.Prepositions)
			{
				var pNode = prepNode.AddNode($"[italic]{prep.EscapeMarkup()}[/]");
				RenderNounPhraseTree(pNode, phrase);
			}
		}
		else
		{
			intentNode.AddNode($"[{RenderingColors.Preposition}]Prepositions[/]: {RenderingColors.NoneText}");
		}
	}

	private static void RenderNounPhraseTree(TreeNode parent, NounPhrase phrase)
	{
		parent.AddNode($"Text: {RenderingColors.FormatDim(phrase.Text.EscapeMarkup())}");
		parent.AddNode($"Head: [{RenderingColors.Bold}]{phrase.Head.EscapeMarkup()}[/]");

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
				RenderNounPhraseTree(pNode, np);
			}
		}
		else
		{
			parent.AddNode($"Complements: {RenderingColors.NoneText}");
		}
	}

	private static string GetPosColor(NlpPartOfSpeech pos)
	{
		return pos switch
		{
			NlpPartOfSpeech.Noun => RenderingColors.Head,
			NlpPartOfSpeech.Verb => RenderingColors.Verb,
			NlpPartOfSpeech.Adjective => RenderingColors.Modifier,
			NlpPartOfSpeech.Adverb => RenderingColors.Modifier,
			NlpPartOfSpeech.Pronoun => RenderingColors.Subject,
			NlpPartOfSpeech.Determiner => RenderingColors.Dim,
			_ => RenderingColors.None
		};
	}
}