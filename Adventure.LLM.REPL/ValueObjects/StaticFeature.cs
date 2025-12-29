namespace Adventure.LLM.REPL.ValueObjects;

public record StaticFeature
{
	public string Type { get; set; } = string.Empty;
	public FeatureFacts Facts { get; set; } = new();
}
