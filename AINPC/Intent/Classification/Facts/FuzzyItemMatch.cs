namespace AINPC.Intent.Classification.Facts;

internal sealed class FuzzyItemMatch(string itemName, double score)
{
	public string ItemName { get; } = itemName;
	public double Score { get; } = score;
}
