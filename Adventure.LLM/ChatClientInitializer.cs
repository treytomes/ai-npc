using Adventure.LLM.Ollama;
using Adventure.LLM.Services;
using Microsoft.Extensions.Hosting;

namespace Adventure.LLM;

public sealed class ChatClientInitializer : IHostedService
{
	private readonly ILlmManager _llmManager;
	private readonly ChatClientHolder _holder;
	private readonly OllamaProps _props;

	public ChatClientInitializer(
		OllamaProps props,
		ILlmManager llmManager,
		ChatClientHolder holder)
	{
		_props = props;
		_llmManager = llmManager;
		_holder = holder;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_holder.ReportStatus("Starting Ollama…");
		await _llmManager.InitializeAsync();

		_holder.ReportStatus("Selecting model…");
		_llmManager.SetModel(_props.ModelId);

		_holder.ReportStatus("Creating chat client…");
		var client = await _llmManager.CreateChatClient()
			.ConfigureAwait(false);

		_holder.ReportStatus("Finalizing…");
		_holder.Set(client);
	}

	public Task StopAsync(CancellationToken cancellationToken) =>
		Task.CompletedTask;
}
