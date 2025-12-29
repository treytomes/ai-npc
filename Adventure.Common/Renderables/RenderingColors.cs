using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.Common.Renderables;

public class RenderingColors
{
	/// <summary>
	/// Primary grammatical elements.
	/// </summary>
	public static class Grammar
	{
		public const string Subject = "cyan";
		public const string Verb = "yellow";
		public const string IndirectObject = "magenta";
		public const string DirectObject = "green";
		public const string Preposition = "blue";
	}

	/// <summary>
	/// Noun phrase elements.
	/// </summary>
	public static class NounPhrase
	{
		public const string Head = "green";
		public const string Modifier = "yellow";
		public const string Complement = "cyan";
	}

	/// <summary>
	/// UI elements.
	/// </summary>
	public static class UI
	{
		public const string None = "grey";
		public const string Dim = "dim";
		public const string Bold = "bold";
	}

	// Common formatted strings.
	public const string NoneText = "<none>";
	public const string NoVerbText = "<no verb>";

	// Helper methods for common formatting patterns.
	public static string FormatNone() => $"[{UI.None}]{NoneText}[/]";
	public static string FormatNoVerb() => $"[{UI.None}]{NoVerbText}[/]";
	public static string FormatDim(string text) => $"[{UI.Dim}]{text}[/]";
	public static string FormatColor(string color, string text) => $"[{color}]{text}[/]";
	public static IRenderable EmptyLine() => new Text(string.Empty);
}