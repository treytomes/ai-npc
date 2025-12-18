namespace AINPC.Intent.Classification.Facts;

public sealed class SuppressedIntent(string name)
{
	public string Name { get; } = name;
}
