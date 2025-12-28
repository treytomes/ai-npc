namespace Adventure.Intent.Classification.Facts;

public sealed class NegativeIntentHint(string intent, double strength)
{
	public string Intent { get; } = intent;
	public double Strength { get; } = strength;
}
