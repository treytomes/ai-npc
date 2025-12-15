namespace AINPC.ValueObjects;

record ItemInfo
{
	public required string Name { get; init; }
	public required string Description { get; init; }
	public required Currency Cost { get; init; }
}
