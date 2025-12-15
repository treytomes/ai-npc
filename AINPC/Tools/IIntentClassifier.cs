using AINPC.Entities;

namespace AINPC.Tools;

internal interface IIntentClassifier
{
	/// <summary>
	/// Determine what tools the user might be trying to get the actor to invoke.
	/// </summary>
	IReadOnlyCollection<string> Classify(string userMessage, Actor actor);
}
