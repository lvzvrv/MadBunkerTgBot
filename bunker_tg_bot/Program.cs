using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    record Room(long HostId)
    {
        public ConcurrentBag<long> Participants { get; set; } = new ConcurrentBag<long>();
        public ConcurrentDictionary<long, string> UserNames { get; set; } = new ConcurrentDictionary<long, string>();
        public bool GameStarted { get; set; } = false;
    }

    static readonly ConcurrentDictionary<string, Room> Rooms = new();
    static readonly ConcurrentDictionary<long, string> UserRoomMap = new();

    static async Task Main(string[] args)
    {
        string token = "7932579792:AAE-deIyk4-zvC8YoiRNHa3H4rcZdi-rNms";
        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cts.Token
        );

        Console.WriteLine("Бот запущен. Нажмите Enter для остановки.");
        Console.ReadLine();

        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is { } message && message.Text is { } messageText)
        {
            var chatId = message.Chat.Id;
            var userName = message.From?.Username ?? "Неизвестный";

            if (messageText == "/start")
            {
                var buttons = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Создать комнату", "Присоединиться" }
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendTextMessageAsync(
                    chatId,
                    "Добро пожаловать! Выберите действие:",
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken
                );
            }
            else if (messageText == "Создать комнату")
            {
                if (UserRoomMap.ContainsKey(chatId))
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Вы уже находитесь в комнате. Сначала покиньте текущую комнату, чтобы создать новую.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                string roomId = new Random().Next(100000, 999999).ToString();
                Rooms[roomId] = new Room(chatId);
                Rooms[roomId].Participants.Add(chatId);
                UserRoomMap[chatId] = roomId;

                var buttons = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Начать игру" }
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Комната создана! ID: {roomId}",
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken
                );

                Console.WriteLine($"[LOG] Пользователь {userName} создал комнату {roomId}.");
            }
            else if (messageText == "Присоединиться")
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Используйте команду /join <id комнаты>",
                    cancellationToken: cancellationToken
                );
            }
            else if (messageText.StartsWith("/join "))
            {
                if (UserRoomMap.ContainsKey(chatId))
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Вы уже находитесь в комнате. Сначала покиньте текущую комнату, чтобы присоединиться к другой.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || !Rooms.ContainsKey(parts[1]))
                {
                    await botClient.SendTextMessageAsync(chatId, "Комната с таким ID не найдена.", cancellationToken: cancellationToken);
                    return;
                }

                string roomId = parts[1];
                if (Rooms.TryGetValue(roomId, out var room))
                {
                    if (room.GameStarted)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Игра уже началась, вы не можете присоединиться.", cancellationToken: cancellationToken);
                        return;
                    }

                    if (!room.Participants.Contains(chatId))
                    {
                        room.Participants.Add(chatId);
                        UserRoomMap[chatId] = roomId;

                        await NotifyParticipants(botClient, room, $"@{userName} присоединился к комнате {roomId}!", cancellationToken);
                        Console.WriteLine($"[LOG] Пользователь {userName} присоединился к комнате {roomId}.");
                    }
                }
            }
            else if (messageText == "/members")
            {
                if (!UserRoomMap.TryGetValue(chatId, out var roomId) || !Rooms.TryGetValue(roomId, out var room))
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
                    return;
                }

                var members = room.Participants.Select(id => $"@{botClient.GetChatAsync(id).Result.Username ?? "Unknown"}").ToArray();
                await botClient.SendTextMessageAsync(chatId, "Участники комнаты: " + string.Join(", ", members), cancellationToken: cancellationToken);
            }
            else if (messageText == "/leave")
            {
                if (UserRoomMap.TryRemove(chatId, out var roomId) && Rooms.TryGetValue(roomId, out var room))
                {
                    if (room.HostId == chatId)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Вы не можете покинуть комнату, так как являетесь её хостом.", cancellationToken: cancellationToken);
                        return;
                    }

                    room.Participants = new ConcurrentBag<long>(room.Participants.Where(id => id != chatId));
                    await NotifyParticipants(botClient, room, $"@{userName} покинул комнату.", cancellationToken);
                    Console.WriteLine($"[LOG] Пользователь {userName} покинул комнату {roomId}.");

                    await botClient.SendTextMessageAsync(chatId, "Вы покинули комнату.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
                }
            }
            else if (messageText == "/delete")
            {
                if (UserRoomMap.TryGetValue(chatId, out var roomId) && Rooms.TryGetValue(roomId, out var room) && room.HostId == chatId)
                {
                    foreach (var participant in room.Participants)
                    {
                        UserRoomMap.TryRemove(participant, out _);
                        await botClient.SendTextMessageAsync(participant, "Комната была удалена хостом.", cancellationToken: cancellationToken);
                    }

                    Rooms.TryRemove(roomId, out _);
                    Console.WriteLine($"[LOG] Комната {roomId} была удалена хостом {userName}.");

                    await botClient.SendTextMessageAsync(chatId, "Вы удалили комнату.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы не являетесь хостом комнаты.", cancellationToken: cancellationToken);
                }
            }
            else if (messageText == "Начать игру")
            {
                if (!UserRoomMap.TryGetValue(chatId, out var roomId) || !Rooms.TryGetValue(roomId, out var room))
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
                    return;
                }

                if (room.HostId != chatId)
                {
                    await botClient.SendTextMessageAsync(chatId, "Только хост комнаты может начать игру.", cancellationToken: cancellationToken);
                    return;
                }

                room.GameStarted = true;

                await NotifyParticipants(botClient, room, "Игра началась! Введите ваше имя.", cancellationToken);
                Console.WriteLine($"[LOG] Игра в комнате {roomId} началась.");
            }
            else if (Rooms.Values.Any(r => r.GameStarted && r.Participants.Contains(chatId)))
            {
                if (UserRoomMap.TryGetValue(chatId, out var roomId) && Rooms.TryGetValue(roomId, out var room))
                {
                    room.UserNames[chatId] = messageText;

                    if (room.UserNames.Count == room.Participants.Count)
                    {
                        var names = room.UserNames.Select(kv => $"@{botClient.GetChatAsync(kv.Key).Result.Username}: {kv.Value}").ToArray();
                        await NotifyParticipants(botClient, room, "Имена участников: \n" + string.Join("\n", names), cancellationToken);
                        Console.WriteLine($"[LOG] Все участники комнаты {roomId} ввели свои имена.");
                    }
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Неизвестная команда.", cancellationToken: cancellationToken);
            }
        }
    }

    static async Task NotifyParticipants(ITelegramBotClient botClient, Room room, string message, CancellationToken cancellationToken)
    {
        var tasks = room.Participants.Select(participant =>
            botClient.SendTextMessageAsync(participant, message, cancellationToken: cancellationToken));
        await Task.WhenAll(tasks);
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[ERROR] Произошла ошибка: {exception}");
        return Task.CompletedTask;
    }
}
