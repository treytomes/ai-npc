using Catalyst;

namespace Adventure.NLP.REPL.SystemIntent;

public abstract class SystemCommandBase(Action<IReadOnlyDictionary<string, object?>?> action) : ISystemCommand
{
	private Action<IReadOnlyDictionary<string, object?>?> _action = action ?? throw new ArgumentNullException(nameof(action));

	public abstract bool CanExecute(ParsedInput input, IntentSeed seed);

	public void Execute(IReadOnlyDictionary<string, object?>? parameters)
	{
		_action(parameters);
	}

	protected static string? ResolveTarget(ParsedInput input, IntentSeed seed)
	{
		// 1. Preferred: noun phrase head
		if (seed.DirectObject?.Head != null)
			return seed.DirectObject.Head;

		// 2. Modifier-only NP (rare but possible)
		if (seed.DirectObject?.Modifiers.Count > 0)
			return seed.DirectObject.Modifiers[0];

		// 3. FINAL FALLBACK: first meaningful token
		foreach (var token in input.ParsedTokens)
		{
			switch (token.Pos)
			{
				case NlpPartOfSpeech.Noun:
				case NlpPartOfSpeech.Adjective:
					return token.Lemma ?? token.Value;
			}
		}

		return null;
	}

	public virtual IReadOnlyDictionary<string, object?>? ParseParameters(ParsedInput input, IntentSeed seed) => null;
}
