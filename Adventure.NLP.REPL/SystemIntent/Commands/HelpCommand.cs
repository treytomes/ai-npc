namespace Adventure.NLP.REPL.SystemIntent.Commands;

public sealed class HelpCommand(Action<IReadOnlyDictionary<string, object?>?> action) : SystemCommandBase(action)
{
	public override bool CanExecute(ParsedInput input, IntentSeed seed)
	{
		var target = ResolveTarget(input, seed);
		var verb = seed.Verb;
		return verb == "help" || target == "help";
	}
}

