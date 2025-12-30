namespace Adventure.LLM.REPL;

public static class IntentTypes
{
	public const string Look = "look";
	public const string Examine = "examine";
	public const string Go = "go";
	public const string Take = "take";
	public const string Use = "use";
	public const string Smell = "smell";
	public const string Listen = "listen";
	public const string Touch = "touch";
	public const string Unknown = "unknown";

	public static readonly HashSet<string> ValidIntents = new()
	{
		Look, Examine, Go, Take, Use, Smell, Listen, Touch
	};
}