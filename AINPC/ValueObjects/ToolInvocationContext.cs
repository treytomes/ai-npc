namespace AINPC.ValueObjects;

internal record ToolInvocationContext
{
	public IEnumerable<ItemResolutionResult>? ResolvedItemResults { get; init; }
}
