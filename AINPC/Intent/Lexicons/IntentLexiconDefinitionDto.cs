using System.Text.Json.Serialization;

namespace AINPC.Intent.Lexicons;

/// <summary>
/// Defines a single intent with its patterns.
/// </summary>
internal sealed record IntentLexiconDefinitionDto
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("patterns")]
	public List<string> Patterns { get; set; } = new();
}