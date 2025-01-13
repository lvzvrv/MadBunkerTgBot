using System.Collections.Concurrent;

namespace bunker_tg_bot.Models
{
    public record Room(long HostId)
    {
        public ConcurrentBag<long> Participants { get; set; } = new ConcurrentBag<long>();
        public ConcurrentDictionary<long, string> UserNames { get; set; } = new ConcurrentDictionary<long, string>();
        public ConcurrentDictionary<long, Character> UserCharacters { get; set; } = new ConcurrentDictionary<long, Character>();
        public ConcurrentDictionary<long, string> UserEditState { get; set; } = new ConcurrentDictionary<long, string>();
        public bool GameStarted { get; set; } = false;
        public GameMode GameMode { get; set; } = GameMode.Quick;  // Default game mode
    }

    public static class RoomManager
    {
        public static readonly ConcurrentDictionary<string, Room> Rooms = new();
        public static readonly ConcurrentDictionary<long, string> UserRoomMap = new();
    }
}