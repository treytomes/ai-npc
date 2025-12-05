using AINPC.Templates;

namespace AINPC;

class RoleFactory
{
	#region Fields

	private readonly CharacterFactory _characters;
	private readonly VillageFactory _villages;
	private readonly TemplateEngine _engine = new TemplateEngine();

	#endregion

	#region Constructors

	public RoleFactory(CharacterFactory characters, VillageFactory villages)
	{
		_characters = characters;
		_villages = villages;
	}

	#endregion

	#region Methods

	public string CreateHelpfulAssistantPrompt()
	{
		return @"You are a helpful assistant.

Use tools only when they are the best way to answer the user's request.
Do not look for excuses to call a tool. Only call one when:
- the user asks for information that the tool directly provides, or
- the user explicitly requests the tool.

For weather questions, call the GetWeather tool only when the user
is clearly asking for actual weather information (current or forecast).
If the user is speaking metaphorically or casually, do not call the tool.

When no tool is appropriate, answer from your own knowledge.
If you do not know something, say so briefly and continue.

Keep responses short and direct unless the user asks for more detail.
Avoid repeating the same information unless the user requests it.

If the user asks you to think, reflect, explain, or discuss ideas,
respond normally—tools are not needed for general conversation.
";
	}

	public string CreateGatekeeperPrompt()
	{
		var character = _characters.GetBramwellHolt();
		var village = _villages.GetElderwood();

		return _engine.Render(
			NPCTemplates.Gatekeeper,
			new Dictionary<string, string>
			{
				["CharacterName"] = character.Name,
				["PersonalityTraits"] = string.Join(", ", character.PersonalityTraits),
				["VillageName"] = village.Name,
				["VillageLocation"] = village.Location,
				["VillageTraits"] = string.Join(", ", village.Traits),
				["VillageEvents"] = village.RecentEvents
			}
		);
	}

	#endregion
}
