namespace LLM.Intent.Entities;

/// <remarks>Roughly duplicated from AINPC.Entities.Actor.
internal class Actor
{
	public required string Role { get; init; }
	public required IReadOnlyList<Items.ValueObjects.ItemInfo> Inventory { get; init; }
}