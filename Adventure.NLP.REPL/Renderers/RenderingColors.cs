namespace Adventure.NLP.REPL.Renderables;

public class RenderingColors : Common.Renderables.RenderingColors
{
	/// <summary>
	/// Gets the appropriate color for a part of speech.
	/// </summary>
	public static string GetPosColor(NlpPartOfSpeech pos) => pos switch
	{
		NlpPartOfSpeech.Noun => NounPhrase.Head,
		NlpPartOfSpeech.Verb => Grammar.Verb,
		NlpPartOfSpeech.Adjective => NounPhrase.Modifier,
		NlpPartOfSpeech.Adverb => NounPhrase.Modifier,
		NlpPartOfSpeech.Pronoun => Grammar.Subject,
		NlpPartOfSpeech.Determiner => UI.Dim,
		_ => UI.None
	};
}