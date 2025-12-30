using Adventure.LLM.REPL.Persistence;
using Adventure.LLM.REPL.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Adventure.LLM.REPL.Services;

public sealed class RoomNavigationService(
		ILogger<RoomNavigationService> logger,
		IRoomRepository roomRepository,
		string initialRoomKey = "main_lab"
) : IRoomNavigationService
{
	private readonly ILogger<RoomNavigationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IRoomRepository _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
	private readonly object _navigationLock = new();
	private string _currentRoomKey = initialRoomKey;

	public string CurrentRoomKey
	{
		get
		{
			lock (_navigationLock)
			{
				return _currentRoomKey;
			}
		}
	}

	public WorldData? CurrentRoom => _roomRepository.GetRoom(CurrentRoomKey);

	public event EventHandler<RoomChangedEventArgs>? RoomChanged;

	public async Task<NavigationResult> NavigateToAsync(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
		{
			return NavigationResult.Failed("No destination specified");
		}

		lock (_navigationLock)
		{
			if (_currentRoomKey.Equals(roomKey, StringComparison.OrdinalIgnoreCase))
			{
				return NavigationResult.Failed("You are already in this room");
			}
		}

		if (!_roomRepository.RoomExists(roomKey))
		{
			return NavigationResult.Failed($"Unknown location: {roomKey}");
		}

		if (!_roomRepository.CanNavigate(CurrentRoomKey, roomKey))
		{
			return NavigationResult.Failed($"You cannot go to {roomKey} from here");
		}

		var previousRoom = CurrentRoomKey;
		var newRoomData = _roomRepository.GetRoom(roomKey);

		lock (_navigationLock)
		{
			_currentRoomKey = roomKey;
		}

		_logger.LogInformation("Player moved from {From} to {To}", previousRoom, roomKey);

		// Raise event
		RoomChanged?.Invoke(this, new RoomChangedEventArgs
		{
			PreviousRoomKey = previousRoom,
			NewRoomKey = roomKey,
			NewRoomData = newRoomData
		});

		await Task.CompletedTask; // For future async operations

		return NavigationResult.Succeeded(roomKey);
	}

	public IEnumerable<string> GetAvailableDestinations()
	{
		return _roomRepository.GetExits(CurrentRoomKey);
	}

	public bool CanNavigateTo(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
			return false;

		return _roomRepository.CanNavigate(CurrentRoomKey, roomKey);
	}
}