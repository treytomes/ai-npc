using Adventure.ValueObjects;

namespace Adventure;

internal interface IItemResolver
{
	ItemResolutionResult Resolve(string userInput, IReadOnlyCollection<ItemInfo> inventory);
}
