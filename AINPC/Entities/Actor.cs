using AINPC.Models;
using AINPC.OllamaRuntime;
using AINPC.Tools;
using AINPC.ValueObjects;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace AINPC.Entities;

internal class Actor : Entity, IHasInventory
{
	#region Fields

	private int _gold = 0;
	private Inventory _inventory = new();
	private readonly RoleInfo _role;
	private readonly IReadOnlyCollection<IOllamaTool> _tools;
	private readonly IIntentClassifier _intentClassifier;
	private Chat? _chat = null;

	#endregion

	#region Constructors

	public Actor(ToolFactory toolFactory, IIntentClassifier intentClassifier, RoleInfo role, IEnumerable<string>? toolNames = null)
	{
		_intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
		_role = role ?? throw new ArgumentNullException(nameof(role));
		_tools = toolFactory.CreateTools(this, toolNames).ToList().AsReadOnly();
	}

	#endregion

	#region Properties

	public string Name => _role.Name;
	public int Gold => _gold;
	public IReadOnlyList<ItemInfo> Inventory => _inventory.ToList().AsReadOnly();

	public bool IsLoaded => _chat != null;

	#endregion

	#region Methods

	public async Task LoadAsync(OllamaRepo ollamaRepo)
	{
		// TODO: This is where we might load a pre-existing chat from file.
		_chat = ollamaRepo.CreateChat(_role.SystemPrompt) ?? throw new NullReferenceException("Unable to initialize chat.");
		await Task.CompletedTask;
	}

	public async Task UnloadAsync()
	{
		// TODO: This is where we might save the chat to a file.
		await Task.CompletedTask;
	}

	public async Task<IAsyncEnumerable<string>> ChatAsync(string message, CancellationToken cancellationToken = default)
	{
		var intents = _intentClassifier.Classify(message, this);

		// Determine required tools based on user intent.
		var requiredTools = _tools
			.Where(t => intents.Contains(t.Intent))
			.ToList();

		// Phase 1: Execute required tools deterministically
		foreach (var tool in requiredTools)
		{
			var result = await tool.InvokeMethodAsync(null);
			if (result != null)
			{
				var resultText = result!.ToString();
				if (!string.IsNullOrWhiteSpace(resultText))
				{
					_chat!.Messages.Add(new Message(
						ChatRole.System,
						$"*This is what you're thinking: {resultText}*"
					));
				}
			}
		}

		// Phase 2: Ask the model to narrate
		return _chat!.SendAsync(message, cancellationToken: cancellationToken);
	}

	public void ReceiveItems(IEnumerable<ItemInfo> items)
	{
		foreach (var item in items)
		{
			ReceiveItem(item);
		}
	}

	public void ReceiveItem(ItemInfo item)
	{
		_inventory.AddItem(item);
	}

	public void LoseItem(ItemInfo item)
	{
		_inventory.RemoveItem(item);
	}

	public bool HasItem(ItemInfo item)
	{
		return _inventory.HasItem(item);
	}

	#endregion
}