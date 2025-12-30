using Adventure.LLM.REPL.ValueObjects;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Concurrent;

namespace Adventure.LLM.REPL.Persistence;

public sealed class YamlRoomRepository : IRoomRepository
{
	private readonly ILogger<YamlRoomRepository> _logger;
	private readonly string _roomsPath;
	private readonly ConcurrentDictionary<string, WorldData> _rooms;
	private readonly IDeserializer _deserializer;

	public YamlRoomRepository(ILogger<YamlRoomRepository> logger, string roomsPath)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_roomsPath = roomsPath ?? throw new ArgumentNullException(nameof(roomsPath));
		_rooms = new ConcurrentDictionary<string, WorldData>(StringComparer.OrdinalIgnoreCase);

		_deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();
	}

	public async Task LoadRoomsAsync(CancellationToken cancellationToken = default)
	{
		_rooms.Clear();

		if (!Directory.Exists(_roomsPath))
		{
			throw new DirectoryNotFoundException($"Rooms directory not found: {_roomsPath}");
		}

		var roomFiles = Directory.GetFiles(_roomsPath, "*.room.yaml");

		if (roomFiles.Length == 0)
		{
			throw new FileNotFoundException($"No room files found in: {_roomsPath}");
		}

		var loadTasks = roomFiles.Select(async file =>
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var yamlContent = await File.ReadAllTextAsync(file, cancellationToken);
				var worldData = _deserializer.Deserialize<WorldData>(yamlContent);

				if (worldData?.Room == null)
				{
					_logger.LogWarning("Invalid room data in file: {File}", file);
					return;
				}

				var roomKey = ExtractRoomKey(file);
				_rooms.TryAdd(roomKey, worldData);

				_logger.LogInformation("Loaded room: {RoomKey} from {File}", roomKey, file);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load room from file: {File}", file);
			}
		});

		await Task.WhenAll(loadTasks);

		_logger.LogInformation("Loaded {Count} room(s) successfully", _rooms.Count);
	}

	public WorldData? GetRoom(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
			return null;

		return _rooms.TryGetValue(roomKey, out var room) ? room : null;
	}

	public IEnumerable<string> GetRoomKeys()
	{
		return _rooms.Keys.ToList();
	}

	public bool RoomExists(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
			return false;

		return _rooms.ContainsKey(roomKey);
	}

	public IReadOnlyDictionary<string, WorldData> GetAllRooms()
	{
		return new Dictionary<string, WorldData>(_rooms);
	}

	public bool CanNavigate(string fromRoom, string toRoom)
	{
		if (string.IsNullOrWhiteSpace(fromRoom) || string.IsNullOrWhiteSpace(toRoom))
			return false;

		// For now, allow navigation between any existing rooms
		// In the future, this could check room connections/exits
		return _rooms.ContainsKey(fromRoom) && _rooms.ContainsKey(toRoom);

		// Future implementation could check:
		// var room = GetRoom(fromRoom);
		// return room?.Room?.Exits?.Any(e => e.Target.Equals(toRoom, StringComparison.OrdinalIgnoreCase)) ?? false;
	}

	public IEnumerable<string> GetExits(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
			return Enumerable.Empty<string>();

		// For now, return all other rooms as possible exits
		// In the future, this should read from room exit data
		return _rooms.Keys.Where(k => !k.Equals(roomKey, StringComparison.OrdinalIgnoreCase));

		// Future implementation:
		// var room = GetRoom(roomKey);
		// return room?.Room?.Exits?.Select(e => e.Target) ?? Enumerable.Empty
		// return room?.Room?.Exits?.Select(e => e.Target) ?? Enumerable.Empty<string>();
	}

	public async Task ReloadRoomsAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Reloading all rooms...");
		await LoadRoomsAsync(cancellationToken);
	}

	private static string ExtractRoomKey(string filePath)
	{
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		return fileName.Replace(".room", "", StringComparison.OrdinalIgnoreCase);
	}
}