namespace AINPC.Intent.Lexicons;

internal interface IIntentLexicon
{
	/// <summary>
	/// Gets the intent patterns.
	/// </summary>
	IReadOnlyList<IntentLexiconDefinition> Intents { get; }
}
