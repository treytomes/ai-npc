namespace LLM.Intent.Classification;

internal static class ShopkeeperNegativeIntentLexicon
{
	public static readonly IReadOnlyDictionary<string, string[]> Intents =
		new Dictionary<string, string[]>
		{
			["item.describe"] =
				[
					"not buying",
					"just looking",
					"don't need details",
					"no need to explain"
				],
			["shop.inventory.list"] =
				[
					"not shopping",
					"just browsing",
					"not interested in buying"
				]
		};
}
