namespace LLM.NLP;

public sealed class IntentSeed(string? verb, IReadOnlyList<string> objects)
{
	public string? Verb { get; } = verb;
	public IReadOnlyList<string> Objects { get; } = objects;
}
