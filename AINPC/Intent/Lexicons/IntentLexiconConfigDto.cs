using System.Text.Json.Serialization;

namespace AINPC.Intent.Lexicons;

/// <summary>
/// Configuration model for intents.
/// </summary>
internal sealed record IntentLexiconConfigDto
{
	[JsonPropertyName("intents")]
	public List<IntentLexiconDefinitionDto> Intents { get; set; } = new();
}
