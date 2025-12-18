using NRules;

namespace LLM.Intent.Classification.Factories;

internal interface IActorSessionFactory
{
	ISession CreateSession(string actorRole);
}