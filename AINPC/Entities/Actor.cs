using AINPC.Enums;
using AINPC.Intent.Classification;
using AINPC.Intent.Classification.Facts;
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
	private readonly IIntentEngine<Actor> _intentEngine;
	private readonly IItemResolver _itemResolver;
	private Chat? _chat = null;

	#endregion

	#region Constructors

	public Actor(ToolFactory toolFactory, IIntentEngine<Actor> intentEngine, IItemResolver itemResolver, string name, RoleInfo role, IEnumerable<string>? toolNames = null)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException(nameof(name));
		}
		_name = name;

		_intentEngine = intentEngine ?? throw new ArgumentNullException(nameof(intentEngine));
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

	private RecentIntent? _recentIntent = null;

	public async Task<IAsyncEnumerable<string>> ChatAsync(
		string message,
		CancellationToken cancellationToken = default)
	{
		var intentResult = _intentEngine.Process(
			message,
			this,
			new IntentEngineContext
			{
				RecentIntent = _recentIntent
			});

		_recentIntent = intentResult.UpdatedRecentIntent;

		var intents = intentResult.Intents;
		var intentNames = intents.Select(i => i.Name).ToHashSet();

		// --------------------------------------------------
		// Step 1: Resolve item if required
		// --------------------------------------------------

		var itemResolutionResults = intents
			.Where(i => i.HasSlot("item_name"))
			.Select(i => i.Slots["item_name"])
			.Select(itemName => _itemResolver.Resolve(itemName, Inventory));
		foreach (var result in itemResolutionResults)
		{
			switch (result.Status)
			{
				case ItemResolutionStatus.NotFound:
					_chat!.Messages.Add(new Message(
						ChatRole.System,
						"FACT: The shop does not carry an item matching the customer's description."
					));
					break;

				case ItemResolutionStatus.Ambiguous:
					var options = string.Join(", ",
						result.Candidates.Select(i => i.Name));

					_chat!.Messages.Add(new Message(
						ChatRole.System,
						$"FACT: The customer could be referring to any of these items: {options}."
					));
					break;

				case ItemResolutionStatus.ExactMatch:
				case ItemResolutionStatus.SingleAliasMatch:
				case ItemResolutionStatus.SingleFuzzyMatch:
					_chat!.Messages.Add(new Message(
						ChatRole.System,
						$"FACT: The customer mentioned {result.Item!.Name}, {result.Item!.Description}, {result.Item!.Cost}"
					));
					break;
			}
		}

		// --------------------------------------------------
		// Step 2: Execute tools deterministically
		// --------------------------------------------------

		var toolsToRun = _tools
			.Where(t => intentNames.Contains(t.Intent));
		// .OrderBy(t => t.ExecutionOrder); // TODO: Should I care about tool execution order?

		var toolContext = new ToolInvocationContext
		{
			ResolvedItemResults = itemResolutionResults
		};

		foreach (var tool in toolsToRun)
		{
			var toolResult = await tool.InvokeAsync(toolContext);

			if (!string.IsNullOrWhiteSpace(toolResult?.ToString()))
			{
				_chat!.Messages.Add(new Message(
					ChatRole.System,
					$"FACT: {toolResult}"
				));
			}
		}

		// --------------------------------------------------
		// Step 3: Narration
		// --------------------------------------------------

		return _chat!.SendAsync(message, cancellationToken);
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