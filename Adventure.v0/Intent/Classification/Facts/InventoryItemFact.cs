namespace Adventure.Intent.Classification.Facts;

internal sealed class InventoryItemFact(string name, IReadOnlyList<string> aliases)
{
	public string Name { get; } = name.ToLowerInvariant();
	public IReadOnlyList<string> Aliases { get; } = aliases.Select(a => a.ToLowerInvariant()).ToList();
}
