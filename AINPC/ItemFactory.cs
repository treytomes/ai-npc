using AINPC.ValueObjects;

namespace AINPC;

class ItemFactory
{
	public IReadOnlyList<ItemInfo> GetGeneralStoreItems()
	{
		return
		[
			new() { Name = "Bread Loaf", Description = "Freshly baked, still warm.", Cost = new Currency(2) },
			new() { Name = "Cheese Wedge", Description = "Sharp and salty.", Cost = new Currency(3) },
			new() { Name = "Healing Herbs", Description = "Restores minor wounds.", Cost = new Currency(8) },
			new() { Name = "Wool Cloak", Description = "Warm and sturdy.", Cost = new Currency(15) },
			new() { Name = "Rope (20 ft)", Description = "Strong hemp rope.", Cost = new Currency(10) },
			new() { Name = "Small Knife", Description = "Simple but reliable.", Cost = new Currency(6) },
		];
	}
}
