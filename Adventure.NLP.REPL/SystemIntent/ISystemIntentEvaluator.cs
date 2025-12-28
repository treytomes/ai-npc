namespace Adventure.NLP.REPL.SystemIntent;

public interface ISystemIntentEvaluator
{
	bool TryEvaluate(ParsedInput input);

	void AddCommand(ISystemCommand command);
	void AddCommands(IEnumerable<ISystemCommand> commands);
}
