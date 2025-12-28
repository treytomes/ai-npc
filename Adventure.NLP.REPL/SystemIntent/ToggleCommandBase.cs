namespace Adventure.NLP.REPL.SystemIntent;

public abstract class ToggleCommandBase(Action<IReadOnlyDictionary<string, object?>?> action) : SystemCommandBase(action)
{
	public override IReadOnlyDictionary<string, object?>? ParseParameters(ParsedInput input, IntentSeed seed)
	{
		bool? enabled = seed.Verb switch
		{
			"show" => true,
			"hide" => false,
			"toggle" or _ => null,
		};

		return new Dictionary<string, object?>
		{
			["enabled"] = enabled
		};
	}
}
