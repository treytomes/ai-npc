using AINPC.ValueObjects;

namespace AINPC;

internal interface IItemResolver
{
	ItemResolutionResult Resolve(string userInput, IReadOnlyCollection<ItemInfo> inventory);
}
