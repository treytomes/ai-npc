using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.NLP.REPL.Renderables;

public class IntentAnalysisRenderable : Common.Renderables.Renderable
{
	private readonly IntentSeed _seed;
	private readonly string _input;
	private readonly ParsedInput _parsed;

	public IntentAnalysisRenderable(IntentSeed seed, string input, ParsedInput parsed)
	{
		_seed = seed ?? throw new ArgumentNullException(nameof(seed));
		_input = input ?? throw new ArgumentNullException(nameof(input));
		_parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new Rule($"[{RenderingColors.UI.Bold} {RenderingColors.Grammar.Verb}]Intent Analysis[/] — \"{_input.EscapeMarkup()}\"")
			.LeftJustified();

		yield return RenderingColors.EmptyLine();

		yield return new TokenTableRenderable(_parsed, "Parsed Tokens");

		yield return RenderingColors.EmptyLine();

		yield return new IntentSeedRenderable(_seed, "Intent Seed", detailed: true);
	}
}