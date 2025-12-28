namespace Adventure.Intent.Lexicons;

internal sealed class IntentLexiconFactory : IIntentLexiconFactory
{
	#region Fields

	private Dictionary<string, IIntentLexicon> _cache = new();

	#endregion

	#region Methods

	public IIntentLexicon GetLexicon(string filename)
	{
		if (!_cache.ContainsKey(filename))
		{
			_cache.Add(filename, IntentLexicon.Load(filename));
		}
		return _cache[filename];
	}

	#endregion
}
