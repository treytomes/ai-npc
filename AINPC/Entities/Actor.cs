using AINPC.Enums;
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

	private string _name;
	private int _gold = 0;
	private Inventory _inventory = new();
	private readonly RoleInfo _role;
	private readonly IReadOnlyCollection<IActorTool> _tools;
	private readonly IIntentClassifier _intentClassifier;
	private readonly ItemResolver _itemResolver;
	private Chat? _chat = null;

	#endregion

	#region Constructors

	public Actor(ToolFactory toolFactory, IIntentClassifier intentClassifier, ItemResolver itemResolver, string name, RoleInfo role, IEnumerable<string>? toolNames = null)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException(nameof(name));
		}
		_name = name;

		_intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
		_itemResolver = itemResolver ?? throw new ArgumentNullException(nameof(itemResolver));
		_role = role ?? throw new ArgumentNullException(nameof(role));
		_tools = toolFactory.CreateTools(this, toolNames).ToList().AsReadOnly();
	}

	#endregion

	#region Properties

	public string Name => _name;
	public RoleInfo Role => _role;
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

		// Step 1: resolve item if needed
		ItemResolutionResult? itemResolution = null;

		if (intents.Contains("item.describe"))
		{
			// var itemNames = string.Join(',', Inventory.Select(x => x.Name));
			// var stream = _chat!.Client.GenerateAsync($"The user said \"{message}\".  You have these items: {itemNames}.  What is the user probably wanting to know more about?  Respond by simply stating the name of the item.");
			// var response = new StringBuilder();
			// await foreach (var chunk in stream)
			// {
			// 	if (chunk == null) continue;
			// 	response.Append(chunk.Response);
			// }
			// Console.WriteLine($"***item completion: {response}***");
			itemResolution = _itemResolver.Resolve(message, Inventory);
			Console.WriteLine($"***item.describe: {itemResolution}***");
		}

		// Step 2: handle resolution outcomes BEFORE tool execution
		if (itemResolution != null)
		{
			switch (itemResolution.Status)
			{
				case ItemResolutionStatus.NotFound:
					_chat!.Messages.Add(new Message(
						ChatRole.System,
						"*You do not have an item matching that description.*"
					));
					return _chat.SendAsync(message, cancellationToken);

				case ItemResolutionStatus.Ambiguous:
					var names = string.Join(", ",
						itemResolution.Candidates.Select(i => i.Name));

					_chat!.Messages.Add(new Message(
						ChatRole.System,
						$"*The customer could mean any of these items: {names}.*"
					));
					return _chat.SendAsync(message, cancellationToken);
			}
		}

		// Step 3: Determine required tools based on user intent.
		var requiredTools = _tools.Where(t => intents.Contains(t.Intent));

		var context = new ToolInvocationContext
		{
			ResolvedItem = itemResolution?.Item
		};

		// Phase 1: Execute required tools deterministically.
		foreach (var tool in requiredTools)
		{
			var result = await tool.InvokeAsync(context);

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

		// Phase 2: Ask the model to narrate.
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