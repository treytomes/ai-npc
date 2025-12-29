namespace Adventure.LLM.REPL.ValueObjects;

public record Room
{
	public string Name { get; set; } = string.Empty;
	public SpatialSummary SpatialSummary { get; set; } = new();
	public List<StaticFeature> StaticFeatures { get; set; } = new();
	public AmbientDetails AmbientDetails { get; set; } = new();
}
