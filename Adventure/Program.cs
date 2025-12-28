using Adventure.States;

namespace Adventure;

class Program
{
	static async Task Main(params string[] args)
	{
		await Bootstrap.Start<AppSettings, OllamaAppEngine, MainMenuState>(args);
	}
}
