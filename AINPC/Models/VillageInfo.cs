namespace AINPC.Models;

record VillageInfo
{
	public string Name { get; init; }
	public string Location { get; init; }
	public IReadOnlyCollection<string> Traits { get; init; }
	public string RecentEvents { get; init; }
}
