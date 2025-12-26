namespace LLM.NLP.REPL.SystemIntent.Commands;

public class ToggleTreeCommand(Action<IReadOnlyDictionary<string, object?>?> action) : ToggleCommandBase(action)
{
	public override bool CanExecute(ParsedInput input, IntentSeed seed)
	{
		var target = ResolveTarget(input, seed);
		return target == "tree";
	}
}
