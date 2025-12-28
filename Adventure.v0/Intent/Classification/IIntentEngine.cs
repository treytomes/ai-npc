namespace Adventure.Intent.Classification;

internal interface IIntentEngine<TActor>
{
	Task<IntentEngineResult> ProcessAsync(
		string input,
		TActor actor,
		IntentEngineContext? context = null);
}