namespace Adventure.NLP.REPL;

internal static class Program
{
	static async Task Main(params string[] args)
	{
		await new Bootstrap().Start<AppSettings, AppEngine, MainAppState>(args);
	}
}
