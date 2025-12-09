namespace AINPC.Models;

record VillageInfo
{
	public required string Name { get; init; }
	public required string Location { get; init; }
	public required IReadOnlyCollection<string> Traits { get; init; }
	public required string RecentEvents { get; init; }
}
