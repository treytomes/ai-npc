using AINPC.Entities;
using AINPC.Tools;

namespace AINPC;

class ActorFactory
{
	#region Fields

	private readonly RoleFactory _roles;
	private readonly ToolFactory _tools;
	private readonly ItemFactory _items;
	private readonly IIntentClassifier _intentClassifier;
	private readonly ItemResolver _itemResolver;

	#endregion

	#region Constructors

	public ActorFactory(RoleFactory roles, ToolFactory tools, ItemFactory items, IIntentClassifier intentClassifier, ItemResolver itemResolver)
	{
		_roles = roles ?? throw new ArgumentNullException(nameof(roles));
		_tools = tools ?? throw new ArgumentNullException(nameof(tools));
		_items = items ?? throw new ArgumentNullException(nameof(items));
		_intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
		_itemResolver = itemResolver ?? throw new ArgumentNullException(nameof(itemResolver));
	}

	#endregion

	#region Methods

	public Actor CreateHelpfulAssistantPrompt()
	{
		return new Actor(
			_tools, _intentClassifier, _itemResolver,
			_roles.CreateHelpfulAssistantPrompt(),
			[GetWeatherTool.NAME]
		);
	}

	public Actor CreateGatekeeper()
	{
		return new Actor(
			_tools, _intentClassifier, _itemResolver,
			_roles.CreateGatekeeper()
		);
	}

	public Actor CreateShopkeeperPrompt()
	{
		var actor = new Actor(
			_tools, _intentClassifier, _itemResolver,
			_roles.CreateShopkeeperPrompt(),
			[GetShopInventoryTool.NAME]
		);

		var items = _items.GetGeneralStoreItems();
		actor.ReceiveItems(items);
		return actor;
	}

	#endregion
}
