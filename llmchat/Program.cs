using Adventure;

namespace llmchat;

internal static class Program
{
	public static async Task Main(params string[] args)
	{
		await new Bootstrap().Start<AppSettings, TerminalGuiAppEngine, MainAppState>(args);
	}
}