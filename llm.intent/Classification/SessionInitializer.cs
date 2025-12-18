using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.Facts;
using NRules;

namespace LLM.Intent.Classification;

internal sealed class SessionInitializer
	: ISessionInitializer<Actor>
{
	public void Initialize(
		ISession session,
		string utterance,
		Actor actor,
		RecentIntent? recentIntent)
	{
		session.Insert(new UserUtterance(utterance));
		session.Insert(new ActorRole(actor.Role));

		if (recentIntent != null)
		{
			session.Insert(recentIntent);
		}
	}
}
