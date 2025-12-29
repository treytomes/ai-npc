using Adventure.NLP.REPL.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL;

public static class RenderableExtensions
{
	// NounPhrase extensions
	public static IRenderable ToRenderable(this NounPhrase phrase, string? title = null)
		=> new NounPhraseRenderable(phrase, title);

	public static IRenderable ToCompactRenderable(this NounPhrase phrase, int indent = 0)
		=> new NounPhraseRenderable(phrase, compact: true);

	// IntentSeed extensions
	public static IRenderable ToRenderable(this IntentSeed seed, string? title = null)
		=> new IntentSeedRenderable(seed, title);

	public static IRenderable ToDetailedRenderable(this IntentSeed seed, string? title = null)
		=> new IntentSeedRenderable(seed, title, detailed: true);

	public static IRenderable ToAnalysisRenderable(this IntentSeed seed, string input, ParsedInput parsed)
		=> new IntentAnalysisRenderable(seed, input, parsed);

	// ParsedInput extensions
	public static IRenderable ToSnapshotRenderable(this ParsedInput parsed, string? rawInput = null, string? title = null)
		=> new ParsedInputSnapshotRenderable(parsed, rawInput, title);

	public static IRenderable ToTokenTableRenderable(this ParsedInput parsed, string? title = null)
		=> new TokenTableRenderable(parsed, title);

	public static IRenderable ToCompactRenderable(this ParsedInput parsed)
	{
		if (parsed.Tokens.Count == 0)
		{
			return new Markup(RenderingColors.FormatNone());
		}

		var parts = new List<string>();
		for (var i = 0; i < parsed.ParsedTokens.Count; i++)
		{
			var token = parsed.ParsedTokens[i];
			var color = RenderingColors.GetPosColor(token.Pos);
			parts.Add(RenderingColors.FormatColor(color, token.Value.EscapeMarkup()));
		}

		return new Markup(string.Join(" ", parts));
	}

	// Parse tree extensions
	public static IRenderable ToParseTreeRenderable(this ParsedInput parsed, string input, IntentSeed? seed = null)
		=> new ParseTreeRenderable(parsed, input, seed);

	public static IRenderable ToParseTreeRenderable(this IntentSeed seed, string input, ParsedInput parsed)
		=> new ParseTreeRenderable(parsed, input, seed);
}