using AINPC.Models;

namespace AINPC;

class CharacterFactory
{
	public CharacterInfo GetBramwellHolt()
	{
		return new CharacterInfo
		{
			Name = "Bramwell \"Bram\" Holt",
			PersonalityTraits =
			[
				"Gruff but fair",
				"Loyal to the village",
				"Suspicious of outsiders",
				"Speaks plainly and briefly",
				"Not overly dramatic"
			]
		};
	}
}