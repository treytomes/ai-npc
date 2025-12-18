namespace AINPC.Intent.Classification.Facts;

internal sealed class UserUtterance(string text)
{
	public string Text { get; } = text.ToLowerInvariant();
}
