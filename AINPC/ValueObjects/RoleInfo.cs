using AINPC.Tools;

namespace AINPC.ValueObjects;

record RoleInfo
{
	#region Constants

	private const string DEFAULT_NAME = "Gus";
	private const string DEFAULT_SYSTEM_PROMPT = "You are the village idiot.";

	#endregion

	#region Constructors

	public RoleInfo(string name, string systemPrompt, IEnumerable<BaseOllamaTool>? tools = null)
	{
		Name = name ?? DEFAULT_NAME;
		SystemPrompt = systemPrompt ?? DEFAULT_SYSTEM_PROMPT;
		Tools = (tools ?? []).ToList().AsReadOnly();
	}

	#endregion

	#region Properties

	public string Name { get; }

	public string SystemPrompt { get; }

	public IReadOnlyList<BaseOllamaTool> Tools { get; }

	#endregion
}
