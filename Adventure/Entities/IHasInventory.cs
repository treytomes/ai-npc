using Adventure.ValueObjects;

namespace Adventure.Entities;

interface IHasInventory
{
	IReadOnlyList<ItemInfo> Inventory { get; }
}