namespace Adventure.ValueObjects;

internal record ToolInvocationContext
{
	public IEnumerable<ItemResolutionResult>? ResolvedItemResults { get; init; }
}
