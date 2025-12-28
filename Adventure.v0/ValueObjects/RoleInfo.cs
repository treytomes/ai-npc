namespace Adventure.ValueObjects;

record RoleInfo
{
	#region Constants

	private const string DEFAULT_NAME = "idiot";
	private const string DEFAULT_SYSTEM_PROMPT = "You are the village idiot.";

	#endregion

	#region Constructors

	public RoleInfo(string name, string systemPrompt)
	{
		Name = name ?? DEFAULT_NAME;
		SystemPrompt = systemPrompt ?? DEFAULT_SYSTEM_PROMPT;
	}

	#endregion

	#region Properties

	public string Name { get; }

	public string SystemPrompt { get; }

	#endregion
}
