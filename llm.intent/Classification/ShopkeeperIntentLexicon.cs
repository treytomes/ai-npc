namespace LLM.Intent.Classification;

internal static class ShopkeeperIntentLexicon
{
	public static readonly IReadOnlyDictionary<string, string[]> Intents =
		new Dictionary<string, string[]>
		{
			["shop.inventory.list"] =
				[
					"what do you sell",
					"what's for sale",
					"show me your stock",
					"what do you have",
					"inventory",
					"wares",
					"goods for sale"
				],

			["item.describe"] =
				[
					"tell me about",
					"describe",
					"what is",
					"what does it do",
					"what's that",
					"details about"
				]
		};
}
