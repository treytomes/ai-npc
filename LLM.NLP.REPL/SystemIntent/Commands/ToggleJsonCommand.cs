namespace LLM.NLP.REPL.SystemIntent.Commands;

public sealed class ToggleJsonCommand(Action<IReadOnlyDictionary<string, object?>?> action) : ToggleCommandBase(action)
{
	public override bool CanExecute(ParsedInput input, IntentSeed seed)
	{
		var target = ResolveTarget(input, seed);
		return target == "json";
	}

	public override IReadOnlyDictionary<string, object?>? ParseParameters(ParsedInput input, IntentSeed seed)
	{
		var args = base.ParseParameters(input, seed) ?? new Dictionary<string, object?>();
		bool? compact = seed.DirectObject?.Modifiers.Contains("compact");

		return new Dictionary<string, object?>
		{
			["enabled"] = args.GetValueOrDefault("enabled", false, false),
			["compact"] = compact ?? false,
		};
	}
}
