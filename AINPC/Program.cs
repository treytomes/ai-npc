namespace AINPC;

class Program
{
    static async Task Main()
    {
        void Log(string msg) => Console.WriteLine(msg);

        Console.WriteLine("AINPC Ollama Bootstrap Test");
        Console.WriteLine("----------------------------------");

        var http = new HttpClient();

        // Initialize installer + manager
        var installer = new OllamaInstaller(http, Log);
        var manager = new OllamaManager(installer, Log);

        // Ensure Ollama exists
        if (!await manager.EnsureInstalledAsync())
        {
            Console.WriteLine("Installation failed.");
            return;
        }

        // Start server
        if (!await manager.StartServerAsync())
        {
            Console.WriteLine("Failed to start Ollama server.");
            return;
        }

        Console.WriteLine("Ollama server is running.");

        // Pull a model (example)
        Console.WriteLine("\nPulling qwen2.5:0.5b...");
        string pullOutput = await manager.PullModelAsync("qwen2.5:0.5b");
        Console.WriteLine(pullOutput);

        // List installed models
        Console.WriteLine("\nListing models...");
        string models = await manager.ListModelsAsync();
        Console.WriteLine(models);

        // List running models
        Console.WriteLine("\nOllama ps...");
        string ps = await manager.ListRunningAsync();
        Console.WriteLine(ps);

        // Stop server cleanly
        await manager.StopServerAsync();
        Console.WriteLine("Server stopped.");
    }
}
