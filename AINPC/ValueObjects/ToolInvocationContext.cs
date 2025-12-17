namespace AINPC.ValueObjects;

internal record ToolInvocationContext
{
	public ItemInfo? ResolvedItem { get; init; }
}
