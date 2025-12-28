using Catalyst;

namespace LLM.NLP.Test.Helpers;

/// <summary>
/// Fluent helper for building <see cref="ParsedInput"/> instances in tests.
/// </summary>
internal sealed class ParsedInputBuilder
{
	private readonly List<ParsedToken> _tokens = [];

	public ParsedInputBuilder Token(
		string value,
		string? lemma = null,
		PartOfSpeech pos = PartOfSpeech.X)
	{
		_tokens.Add(new ParsedToken(
			Value: value,
			Lemma: lemma ?? value,
			Pos: pos));

		return this;
	}

	public ParsedInput Build()
	{
		var values = _tokens.Select(t => t.Value).ToList();
		var lemmas = _tokens.Select(t => t.Lemma).ToList();

		return new ParsedInput(
			RawText: string.Join(" ", values),
			NormalizedText: string.Join(" ", values),
			Tokens: values,
			Lemmas: lemmas,
			ParsedTokens: _tokens.ToList());
	}
}
