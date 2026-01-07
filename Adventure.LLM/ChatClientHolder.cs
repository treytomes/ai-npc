using Microsoft.Extensions.AI;

namespace Adventure.LLM;

public sealed class ChatClientHolder
{
	public IChatClient? Client { get; private set; }

	private readonly TaskCompletionSource _ready =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	public Task Ready => _ready.Task;

	public event Action<string>? StatusChanged;

	public void ReportStatus(string message)
	{
		StatusChanged?.Invoke(message);
	}

	public void Set(IChatClient client)
	{
		Client = client;
		_ready.TrySetResult();
	}
}
