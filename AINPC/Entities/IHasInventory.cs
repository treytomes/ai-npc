using AINPC.ValueObjects;

namespace AINPC.Entities;

interface IHasInventory
{
	IReadOnlyList<ItemInfo> Inventory { get; }
}