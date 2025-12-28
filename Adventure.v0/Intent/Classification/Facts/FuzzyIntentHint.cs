namespace Adventure.Intent.Classification.Facts;

/// <summary>
/// Represents a fuzzy hint that a user utterance may correspond to an intent.
/// </summary>
internal sealed class FuzzyIntentHint(string intent, double confidence, bool isBiased = false)
{
	public string Intent { get; } = intent;
	public double Confidence { get; private set; } = confidence;
	public bool IsBiased { get; } = isBiased;
}
