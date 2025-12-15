using AINPC.Entities;
using System.Text;

namespace AINPC.Tools;

/// <summary>
/// Tool used by a shopkeeper to list items currently available for sale.
/// </summary>
internal class GetShopInventoryTool : BaseOllamaTool
{
	#region Constants

	public const string NAME = "list_shop_inventory";
	public const string INTENT = "shop.inventory.list";

	#endregion

	#region Fields

	private readonly Actor _shopkeeper;

	#endregion

	#region Constructors

	public GetShopInventoryTool(Actor shopkeeper)
		: base(NAME, "List all items currently for sale in the shop, including prices.", INTENT)
	{
		_shopkeeper = shopkeeper ?? throw new ArgumentNullException(nameof(shopkeeper));
	}

	#endregion

	#region Methods

	protected override async Task<object?> InvokeInternalAsync(
		IDictionary<string, object?> args)
	{
		await Task.CompletedTask;

		if (_shopkeeper.Inventory.Count == 0)
		{
			return "No items are currently for sale.";
		}

		var sb = new StringBuilder();

		// Intentionally structured, not flowery.
		for (var i = 0; i < _shopkeeper.Inventory.Count; i++)
		{
			var item = _shopkeeper.Inventory[i];
			sb.AppendLine($"{i + 1}. {item.Name} - {item.Cost} gold");
		}

		return sb.ToString();
	}
	#endregion
}
