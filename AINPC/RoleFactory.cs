using AINPC.Templates;
using AINPC.Tools;
using AINPC.ValueObjects;

namespace AINPC;

class RoleFactory
{
	#region Fields

	private readonly CharacterFactory _characters;
	private readonly VillageFactory _villages;
	private readonly TemplateEngine _engine = new();

	#endregion

	#region Constructors

	public RoleFactory(CharacterFactory characters, VillageFactory villages)
	{
		_characters = characters ?? throw new ArgumentNullException(nameof(characters));
		_villages = villages ?? throw new ArgumentNullException(nameof(villages));
	}

	#endregion

	#region Methods

	public RoleInfo CreateHelpfulAssistantPrompt()
	{
		var systemPrompt = _engine.Render(NPCTemplates.HelpfulAssistant);
		return new RoleInfo("Assistant", systemPrompt);
	}

	public RoleInfo CreateGatekeeper()
	{
		var character = _characters.GetBramwellHolt();
		var village = _villages.GetElderwood();

		var systemPrompt = _engine.Render(
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

		return new(character.Name, systemPrompt);
	}

	public RoleInfo CreateShopkeeperPrompt()
	{
		var character = _characters.GetMarloweReed();
		var village = _villages.GetElderwood();

		var systemPrompt = _engine.Render(NPCTemplates.Shopkeeper,
			new Dictionary<string, string>
			{
				["CharacterName"] = character.Name,
				["PersonalityTraits"] = string.Join(", ", character.PersonalityTraits),
				["VillageName"] = village.Name,
				["VillageLocation"] = village.Location,
				["VillageTraits"] = string.Join(", ", village.Traits),
				["VillageEvents"] = village.RecentEvents
			});

		return new(character.Name, systemPrompt);
	}

	#endregion
}
