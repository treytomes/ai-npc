namespace LLM.Intent.Entities;

/// <remarks>Roughly duplicated from AINPC.Entities.Actor.
internal class Actor
{
	public required IReadOnlyList<Items.ValueObjects.ItemInfo> Inventory { get; init; }
}