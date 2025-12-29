namespace Adventure.LLM.REPL;

internal static class Program
{
	public static async Task Main(params string[] args)
	{
		await new Bootstrap().Start<AppSettings, LlmAppEngine, MainAppState>(args);
	}
}
