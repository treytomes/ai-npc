using AINPC.Models;

namespace AINPC;

class ItemFactory
{
	public IReadOnlyList<Item> GetGeneralStoreItems()
	{
		return
		[
			new() { Name = "Bread Loaf", Description = "Freshly baked, still warm.", Cost = 2 },
			new() { Name = "Cheese Wedge", Description = "Sharp and salty.", Cost = 3 },
			new() { Name = "Healing Herbs", Description = "Restores minor wounds.", Cost = 8 },
			new() { Name = "Wool Cloak", Description = "Warm and sturdy.", Cost = 15 },
			new() { Name = "Rope (20 ft)", Description = "Strong hemp rope.", Cost = 10 },
			new() { Name = "Small Knife", Description = "Simple but reliable.", Cost = 6 },
		];
	}
}
