namespace Adventure.NLP.SystemIntent;

public interface ISystemCommand
{
	bool CanExecute(ParsedInput input, IntentSeed seed);
	void Execute(IReadOnlyDictionary<string, object?>? parameters);
	IReadOnlyDictionary<string, object?>? ParseParameters(ParsedInput input, IntentSeed seed);
}

