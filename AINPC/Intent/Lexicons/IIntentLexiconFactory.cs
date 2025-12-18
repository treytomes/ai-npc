namespace AINPC.Intent.Lexicons;

internal interface IIntentLexiconFactory
{
	IIntentLexicon GetLexicon(string filename);
}
