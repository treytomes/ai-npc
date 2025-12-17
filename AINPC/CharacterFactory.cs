using AINPC.ValueObjects;

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

	public CharacterInfo GetMarloweReed()
	{
		return new CharacterInfo
		{
			Name = "Marlowe Reed",
			PersonalityTraits =
			[
				"Cheerful and talkative",
				"Knows every rumor in town",
				"Always trying to make a sale",
				"Shrewd but friendly",
				"Speaks with a folksy charm"
			]
		};
	}
}