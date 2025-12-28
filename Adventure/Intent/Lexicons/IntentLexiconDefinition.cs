namespace Adventure.Intent.Lexicons;

internal sealed record IntentLexiconDefinition
{
	public required string Name { get; init; }
	public required IReadOnlyList<string> Patterns { get; init; }
}