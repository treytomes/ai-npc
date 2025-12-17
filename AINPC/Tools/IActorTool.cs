using AINPC.ValueObjects;

namespace AINPC.Tools;

internal interface IActorTool
{
	string Name { get; }
	string Intent { get; }
	Task<string> InvokeAsync(ToolInvocationContext context);
}