using AINPC.ValueObjects;

namespace AINPC;

class ItemFactory
{
	public IReadOnlyList<ItemInfo> GetGeneralStoreItems()
	{
		return
		[
			new ItemInfo
			{
				Name = "Bread Loaf",
				Aliases = ["bread", "loaf", "bread loaf"],
				Description = "Freshly baked, still warm.",
				Cost = new Currency(2)
			},

			new ItemInfo
			{
				Name = "Cheese Wedge",
				Aliases = ["cheese", "cheese wedge", "wedge of cheese"],
				Description = "Sharp and salty.",
				Cost = new Currency(3)
			},

			new ItemInfo
			{
				Name = "Healing Herbs",
				Aliases = ["herbs", "healing herbs", "medicinal herbs"],
				Description = "Restores minor wounds.",
				Cost = new Currency(8)
			},

			new ItemInfo
			{
				Name = "Wool Cloak",
				Aliases = ["cloak", "wool cloak", "cloak wool"],
				Description = "Warm and sturdy.",
				Cost = new Currency(15)
			},

			new ItemInfo
			{
				Name = "Rope (20 ft)",
				Aliases = ["rope", "hemp rope", "20 foot rope", "20 ft rope"],
				Description = "Strong hemp rope.",
				Cost = new Currency(10)
			},

			new ItemInfo
			{
				Name = "Small Knife",
				Aliases = ["knife", "small knife", "dagger"],
				Description = "Simple but reliable.",
				Cost = new Currency(6)
			},
		];
	}
}
