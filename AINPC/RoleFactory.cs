using AINPC.Templates;
using AINPC.ValueObjects;

namespace AINPC;

internal class RoleFactory
{
	#region Fields

	private readonly VillageFactory _villages;
	private readonly TemplateEngine _engine = new();

	#endregion

	#region Constructors

	public RoleFactory(VillageFactory villages)
	{
		_villages = villages ?? throw new ArgumentNullException(nameof(villages));
	}

	#endregion

	#region Methods

	public RoleInfo CreateHelpfulAssistantPrompt()
	{
		var systemPrompt = _engine.Render(NPCTemplates.HelpfulAssistant);
		return new RoleInfo("assistant", systemPrompt);
	}

	public RoleInfo CreateGatekeeper(CharacterInfo character)
	{
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

		return new("gatekeeper", systemPrompt);
	}

	public RoleInfo CreateShopkeeperPrompt(CharacterInfo character)
	{
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

		return new("shopkeeper", systemPrompt);
	}

	#endregion
}
