using AINPC.Entities;

namespace AINPC.Tools;

internal sealed class SimpleIntentClassifier : IIntentClassifier
{
	public IReadOnlyCollection<string> Classify(string userMessage, Actor actor)
	{
		var intents = new List<string>();

		var msg = userMessage.ToLowerInvariant();

		if (msg.Contains("weather") || msg.Contains("temperature") || msg.Contains("rain") || msg.Contains("cold"))
			intents.Add("weather.query");

		if (msg.Contains("buy") || msg.Contains("sale") || msg.Contains("sell") || msg.Contains("price") || msg.Contains("inventory"))
			intents.Add("shop.inventory.list");

		if (msg.ContainsAny("describe", "tell"))
			intents.Add("item.describe");

		return intents;
	}
}
