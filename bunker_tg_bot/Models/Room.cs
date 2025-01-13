using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

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

        public async Task SaveChangesAsync(ITelegramBotClient botClient, long chatId, string message, CancellationToken cancellationToken)
        {
            if (UserEditState.TryGetValue(chatId, out var editField))
            {
                var character = UserCharacters.GetOrAdd(chatId, new Character());
                switch (editField)
                {
                    case "HealthStatus":
                        character.HealthStatus = message;
                        break;
                    case "Job":
                        character.Job = message;
                        break;
                    case "Baggage":
                        character.Baggage = message;
                        break;
                    case "UniqueKnowledge":
                        character.UniqueKnowledge = message;
                        break;
                    case "Age":
                        if (int.TryParse(message, out var age))
                        {
                            character.Age = age;
                        }
                        break;
                    case "Gender":
                        character.Gender = message;
                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, "Неизвестное поле для редактирования.", cancellationToken: cancellationToken);
                        return;
                }

                UserEditState.TryRemove(chatId, out _);
                await botClient.SendTextMessageAsync(chatId, "Изменения сохранены.", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Нет изменений для сохранения.", cancellationToken: cancellationToken);
            }
        }
    }

    public static class RoomManager
    {
        public static readonly ConcurrentDictionary<string, Room> Rooms = new();
        public static readonly ConcurrentDictionary<long, string> UserRoomMap = new();

        public static async Task HandleGameModeSelection(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!UserRoomMap.TryGetValue(chatId, out var roomId) || !Rooms.TryGetValue(roomId, out var room))
            {
                await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
                return;
            }

            if (room.HostId != chatId)
            {
                await botClient.SendTextMessageAsync(chatId, "Только хост может выбрать режим игры.", cancellationToken: cancellationToken);
                return;
            }

            switch (messageText)
            {
                case "Быстрая":
                    room.GameMode = GameMode.Quick;
                    break;
                case "Средняя":
                    room.GameMode = GameMode.Medium;
                    break;
                case "Подробная":
                    room.GameMode = GameMode.Detailed;
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId, "Неизвестный режим игры.", cancellationToken: cancellationToken);
                    return;
            }

            room.GameStarted = true;
            await botClient.SendTextMessageAsync(chatId, $"Режим игры установлен на {messageText}.", cancellationToken: cancellationToken);
            await NotifyParticipants(botClient, room, "Введите имя", cancellationToken);

            Console.WriteLine($"[LOG] Режим игры в комнате {roomId} установлен на {messageText}.");
        }

        private static async Task NotifyParticipants(ITelegramBotClient botClient, Room room, string message, CancellationToken cancellationToken)
        {
            foreach (var participant in room.Participants)
            {
                await botClient.SendTextMessageAsync(participant, message, cancellationToken: cancellationToken);
            }
        }
    }
}

