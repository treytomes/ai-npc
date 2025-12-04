using Microsoft.Extensions.Logging;

namespace AINPC;

class MainState : AppState
{
    #region Fields

    private readonly ILogger<MainState> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly OllamaManager _ollamaManager;

    #endregion

    #region Constructors

    public MainState(ILogger<MainState> logger, IServiceProvider serviceProvider, OllamaManager ollamaManager)
	{
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));	
        _ollamaManager = ollamaManager ?? throw new ArgumentNullException(nameof(ollamaManager));
	}

    #endregion

    #region Methods

	public override async Task RunAsync()
	{
        Console.WriteLine("AINPC Ollama Bootstrap Test");
        Console.WriteLine("----------------------------------");

        // Ensure Ollama exists
        if (!await _ollamaManager.EnsureInstalledAsync())
        {
            Console.WriteLine("Installation failed.");
            return;
        }

        // Start server
        if (!await _ollamaManager.StartServerAsync())
        {
            Console.WriteLine("Failed to start Ollama server.");
            return;
        }

        Console.WriteLine("Ollama server is running.");

        // Pull a model (example)
        Console.WriteLine("\nPulling qwen2.5:0.5b...");
        string pullOutput = await _ollamaManager.PullModelAsync("qwen2.5:0.5b");
        Console.WriteLine(pullOutput);

        // List installed models
        Console.WriteLine("\nListing models...");
        string models = await _ollamaManager.ListModelsAsync();
        Console.WriteLine(models);

        // List running models
        Console.WriteLine("\nOllama ps...");
        string ps = await _ollamaManager.ListRunningAsync();
        Console.WriteLine(ps);

        // Stop server cleanly
        await _ollamaManager.StopServerAsync();
        Console.WriteLine("Server stopped.");
	}
    
    #endregion
}