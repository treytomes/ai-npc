using Adventure.ValueObjects;

namespace Adventure.Tools;

internal interface IActorTool
{
	string Name { get; }
	string Intent { get; }
	Task<string> InvokeAsync(ToolInvocationContext context);
}