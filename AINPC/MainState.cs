using AINPC.OllamaRuntime;
using AINPC.ValueObjects;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using Spectre.Console;

namespace AINPC;

class MainState : AppState
{
	#region Fields

	private readonly ILogger<MainState> _logger;
	private readonly OllamaRepo _ollamaRepo;

	private readonly CharacterFactory _characters;
	private readonly VillageFactory _villages;
	private readonly RoleFactory _roles;

	private RoleInfo _role;
	private Chat? _chat;

	#endregion

	#region Constructors

	public MainState(
		IStateManager states,
		ILogger<MainState> logger,
		OllamaRepo ollamaRepo)
		: base(states)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));

		_characters = new();
		_villages = new();
		_roles = new(_characters, _villages);

		_role = _roles.CreateGatekeeperPrompt();
	}

	#endregion

	#region Methods

	public override async Task LoadAsync()
	{
		_chat = _ollamaRepo.CreateChat(_role.SystemPrompt) ?? throw new NullReferenceException("Unable to initialize chat.");

		AnsiConsole.MarkupLine("[bold]Type your message. Press ENTER on an empty line to quit.[/]\n");
	}

	public override async Task UnloadAsync()
	{
	}

	public override async Task UpdateAsync()
	{
		var userMsg = AnsiConsole.Prompt(
			new TextPrompt<string>("[cyan]You:[/] ")
				.AllowEmpty());

		if (string.IsNullOrWhiteSpace(userMsg))
		{
			await LeaveAsync();
			return;
		}

		AnsiConsole.Markup($"[green]{_role.Name}:[/] ");

		await foreach (var token in _chat!.SendAsync(userMsg, _role.Tools))
		{
			// Stream tokens directly to console
			AnsiConsole.Write(token);
		}

		AnsiConsole.WriteLine();
	}

	#endregion
}
