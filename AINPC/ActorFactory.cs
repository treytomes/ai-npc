using AINPC.Entities;
using AINPC.Intent.Classification;
using AINPC.Tools;

namespace AINPC;

class ActorFactory
{
	#region Fields

	private readonly CharacterFactory _characters;
	private readonly RoleFactory _roles;
	private readonly ToolFactory _tools;
	private readonly ItemFactory _items;
	private readonly IIntentEngine<Actor> _intentEngine;
	private readonly IItemResolver _itemResolver;

	#endregion

	#region Constructors

	public ActorFactory(CharacterFactory characters, RoleFactory roles, ToolFactory tools, ItemFactory items, IIntentEngine<Actor> intentEngine, IItemResolver itemResolver)
	{
		_characters = characters ?? throw new ArgumentNullException(nameof(characters));
		_roles = roles ?? throw new ArgumentNullException(nameof(roles));
		_tools = tools ?? throw new ArgumentNullException(nameof(tools));
		_items = items ?? throw new ArgumentNullException(nameof(items));
		_intentEngine = intentEngine ?? throw new ArgumentNullException(nameof(intentEngine));
		_itemResolver = itemResolver ?? throw new ArgumentNullException(nameof(itemResolver));
	}

	#endregion

	#region Methods

	public Actor CreateHelpfulAssistantPrompt()
	{
		return new Actor(
			_tools, _intentEngine, _itemResolver,
			"Assistant",
			_roles.CreateHelpfulAssistantPrompt(),
			[GetWeatherTool.NAME]
		);
	}

	public Actor CreateGatekeeper()
	{
		var character = _characters.GetBramwellHolt();
		return new Actor(
			_tools, _intentEngine, _itemResolver,
			character.Name,
			_roles.CreateGatekeeper(character)
		);
	}

	public Actor CreateShopkeeperPrompt()
	{
		var character = _characters.GetMarloweReed();

		var actor = new Actor(
			_tools, _intentEngine, _itemResolver,
			character.Name,
			_roles.CreateShopkeeperPrompt(character),
			[GetShopInventoryTool.NAME, DescribeItemTool.NAME]
		);

		var items = _items.GetGeneralStoreItems();
		actor.ReceiveItems(items);
		return actor;
	}

	#endregion
}
