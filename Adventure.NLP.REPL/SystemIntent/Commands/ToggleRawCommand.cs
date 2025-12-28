namespace Adventure.NLP.REPL.SystemIntent.Commands;

public sealed class ToggleRawCommand(Action<IReadOnlyDictionary<string, object?>?> action) : ToggleCommandBase(action)
{
	public override bool CanExecute(ParsedInput input, IntentSeed seed)
	{
		var target = ResolveTarget(input, seed);
		return target == "raw";
	}
}
