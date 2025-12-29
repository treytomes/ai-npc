using Adventure.States;

namespace Adventure;

class Program
{
	static async Task Main(params string[] args)
	{
		await (new BootstrapV0()).Start<AppSettings, OllamaAppEngine, MainMenuState>(args);
	}
}
