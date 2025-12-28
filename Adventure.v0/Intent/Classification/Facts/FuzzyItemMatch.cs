namespace Adventure.Intent.Classification.Facts;

internal sealed class FuzzyItemMatch(string itemName, double score, string originalPhrase, string role)
{
	public string ItemName { get; } = itemName;
	public double Score { get; } = score;

	/// <summary>
	/// The noun phrase the player actually used.
	/// </summary>
	public string OriginalPhrase { get; } = originalPhrase;

	public string Role { get; } = role;
}
