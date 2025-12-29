using Adventure.LLM.Ollama;
using Microsoft.Extensions.Hosting;

namespace Adventure.LLM.Services;

sealed class OllamaLifetimeHook : IHostedService
{
	#region Fields

	private readonly IHostApplicationLifetime _life;
	private readonly OllamaProcessManager _manager;

	#endregion

	#region Constructors

	public OllamaLifetimeHook(IHostApplicationLifetime life, OllamaProcessManager manager)
	{
		_life = life;
		_manager = manager;

		_life.ApplicationStopping.Register(OnStopping);
	}

	#endregion

	#region Methods

	public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	public Task StopAsync(CancellationToken cancellationToken)
	{
		OnStopping();
		return Task.CompletedTask;
	}

	private void OnStopping()
	{
		try
		{
			_manager.StopServer();
		}
		catch { }
	}

	#endregion
}
