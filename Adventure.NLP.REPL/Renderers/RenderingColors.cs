namespace Adventure.NLP.REPL;

public static class RenderingColors
{
	// Primary grammatical elements
	public const string Subject = "cyan";
	public const string Verb = "yellow";
	public const string IndirectObject = "magenta";
	public const string DirectObject = "green";
	public const string Preposition = "blue";

	// Noun phrase elements
	public const string Head = "green";
	public const string Modifier = "yellow";
	public const string Complement = "cyan";

	// UI elements
	public const string None = "grey";
	public const string Dim = "dim";
	public const string Bold = "bold";

	// Common formatted strings
	public const string NoneText = "<none>";
	public const string NoVerbText = "<no verb>";

	// Helper methods for common formatting patterns
	public static string FormatNone() => $"[{None}]{NoneText}[/]";
	public static string FormatNoVerb() => $"[{None}]{NoVerbText}[/]";
	public static string FormatDim(string text) => $"[{Dim}]{text}[/]";
	public static string FormatColor(string color, string text) => $"[{color}]{text}[/]";
}