using AINPC.ValueObjects;

namespace AINPC;

class VillageFactory
{
	public VillageInfo GetElderwood()
	{
		return new VillageInfo
		{
			Name = "Elderwood",
			Location = "in a forest valley near the Greyback Mountains",
			Traits =
			[
				"quiet farms",
				"friendly people",
				"a lumber mill"
			],
			RecentEvents = "a pack of wolves has been attacking livestock"
		};
	}
}