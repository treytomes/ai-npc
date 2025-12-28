using NRules;

namespace Adventure.Intent.Classification.Factories;

internal interface IActorSessionFactory
{
	ISession CreateSession(string actorRole);
}