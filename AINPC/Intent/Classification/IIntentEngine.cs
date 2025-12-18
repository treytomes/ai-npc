namespace AINPC.Intent.Classification;

internal interface IIntentEngine<TActor>
{
	IntentEngineResult Process(
		string input,
		TActor actor,
		IntentEngineContext? context = null);
}