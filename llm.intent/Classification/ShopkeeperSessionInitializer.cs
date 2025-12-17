using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.Facts;
using NRules;

namespace LLM.Intent.Classification;

internal sealed class ShopkeeperSessionInitializer
	: ISessionInitializer<Actor>
{
	public void Initialize(
		ISession session,
		string utterance,
		Actor actor,
		RecentIntent? recentIntent)
	{
		session.Insert(new UserUtterance(utterance));
		session.Insert(new ActorRole("shopkeeper"));

		if (recentIntent != null)
		{
			session.Insert(recentIntent);
		}
	}
}
