using System.Collections;
using AINPC.Entities;
using AINPC.ValueObjects;

namespace AINPC.Models;

internal class Inventory : Entity, IEnumerable<ItemInfo>
{
	#region Fields

	private List<ItemInfo> _items = new();

	#endregion

	#region Properties

	public IReadOnlyList<ItemInfo> Items => _items.AsReadOnly();

	#endregion

	#region Methods

	public IEnumerator<ItemInfo> GetEnumerator()
	{
		return ((IEnumerable<ItemInfo>)_items).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_items).GetEnumerator();
	}

	public void AddItem(ItemInfo item)
	{
		if (HasItem(item)) throw new ArgumentException($"That item is already in the inventory: {item.Name}", nameof(item));
		_items.Add(item);
	}

	public void RemoveItem(ItemInfo item)
	{
		if (!HasItem(item)) throw new ArgumentException($"That item is not in the inventory: {item.Name}", nameof(item));
		_items.Remove(item);
	}

	public bool HasItem(ItemInfo item)
	{
		return _items.Contains(item);
	}

	#endregion
}
