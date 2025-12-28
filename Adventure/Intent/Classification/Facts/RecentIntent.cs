namespace Adventure.Intent.Classification.Facts;

public sealed class RecentIntent(string name, double confidence)
{
	public string Name { get; } = name;
	public double Confidence { get; } = confidence;

	public RecentIntent? Decay(double factor = 0.85)
	{
		var newConfidence = Confidence * factor;
		if (newConfidence < 0.2)
		{
			return null;
		}
		return new RecentIntent(Name, Confidence * factor);
	}
}
