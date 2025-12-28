using Catalyst;

namespace LLM.NLP.Services;

/// <summary>
/// Catalyst-based implementation of <see cref="INlpParser"/>.
/// </summary>
internal sealed class CatalystNlpParser : INlpParser
{
	/// <inheritdoc />
	public ParsedInput Parse(Document document)
	{
		if (document == null)
			throw new ArgumentNullException(nameof(document));

		var rawText = document.Value ?? string.Empty;

		var parsedTokens = document
			.SelectMany(s => s.Tokens)
			.Where(t => t.POS != PartOfSpeech.PUNCT)
			.Select(t => new ParsedToken(
				Value: t.Value.ToLowerInvariant(),
				Lemma: t.Lemma?.ToLowerInvariant() ?? t.Value.ToLowerInvariant(),
				Pos: t.POS))
			.ToList();

		var tokens = new List<string>();
		var lemmas = new List<string>();

		foreach (var span in document)
		{
			foreach (var token in span.Tokens)
			{
				if (token.POS == PartOfSpeech.PUNCT)
					continue;

				var value = token.Value.ToLowerInvariant();
				tokens.Add(value);

				var lemma = token.Lemma?.ToLowerInvariant() ?? value;
				lemmas.Add(lemma);
			}
		}

		var normalizedText = string.Join(" ", tokens);

		return new ParsedInput(
			RawText: rawText,
			NormalizedText: normalizedText,
			Tokens: tokens,
			Lemmas: lemmas,
			ParsedTokens: parsedTokens);
	}
}
