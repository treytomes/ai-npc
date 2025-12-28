using Adventure.Enums;

namespace Adventure.ValueObjects;

internal record ItemResolutionResult
{
	public ItemResolutionStatus Status { get; init; }
	public ItemInfo? Item { get; init; } = null;
	public IReadOnlyList<ItemInfo> Candidates { get; init; } = Array.Empty<ItemInfo>();
}