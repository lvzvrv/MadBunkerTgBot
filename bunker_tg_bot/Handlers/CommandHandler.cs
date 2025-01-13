using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using bunker_tg_bot.Models;
using bunker_tg_bot.Utilities;
using System.Collections.Concurrent;

namespace bunker_tg_bot.Handlers
{
    public static class CommandHandler
    {
        public static async Task StartCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
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

        public static async Task CreateRoomCommand(ITelegramBotClient botClient, long chatId, string userName, CancellationToken cancellationToken)
        {
            if (Room.UserRoomMap.ContainsKey(chatId))
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Вы уже находитесь в комнате. Сначала покиньте текущую комнату, чтобы создать новую.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            string roomId = new Random().Next(100000, 999999).ToString();
            Room.Rooms[roomId] = new Room(chatId);
            Room.Rooms[roomId].Participants.Add(chatId);
            Room.UserRoomMap[chatId] = roomId;

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

        public static async Task JoinCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "Используйте команду /join <id комнаты>",
                cancellationToken: cancellationToken
            );
        }

        public static async Task SelectGameModeCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var buttons = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Быстрая", "Средняя", "Подробная" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId,
                "Выберите режим игры:",
                replyMarkup: buttons,
                cancellationToken: cancellationToken
            );
        }

        public static async Task JoinRoomCommand(ITelegramBotClient botClient, long chatId, string userName, string messageText, CancellationToken cancellationToken)
        {
            if (Room.UserRoomMap.ContainsKey(chatId))
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Вы уже находитесь в комнате. Сначала покиньте текущую комнату, чтобы присоединиться к другой.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !Room.Rooms.ContainsKey(parts[1]))
            {
                await botClient.SendTextMessageAsync(chatId, "Комната с таким ID не найдена.", cancellationToken: cancellationToken);
                return;
            }

            string roomId = parts[1];
            if (Room.Rooms.TryGetValue(roomId, out var room))
            {
                if (room.GameStarted)
                {
                    await botClient.SendTextMessageAsync(chatId, "Игра уже началась, вы не можете присоединиться.", cancellationToken: cancellationToken);
                    return;
                }

                if (!room.Participants.Contains(chatId))
                {
                    room.Participants.Add(chatId);
                    Room.UserRoomMap[chatId] = roomId;

                    await Notifier.NotifyParticipants(botClient, room, $"@{userName} присоединился к комнате {roomId}!", cancellationToken);
                    Console.WriteLine($"[LOG] Пользователь {userName} присоединился к комнате {roomId}.");
                }
            }
        }

        public static async Task MembersCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (!Room.UserRoomMap.TryGetValue(chatId, out var roomId) || !Room.Rooms.TryGetValue(roomId, out var room))
            {
                await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
                return;
            }

            var members = room.Participants.Select(id => $"@{botClient.GetChatAsync(id).Result.Username ?? "Unknown"}").ToArray();
            await botClient.SendTextMessageAsync(chatId, "Участники комнаты: " + string.Join(", ", members), cancellationToken: cancellationToken);
        }

        public static async Task LeaveCommand(ITelegramBotClient botClient, long chatId, string userName, CancellationToken cancellationToken)
        {
            if (Room.UserRoomMap.TryRemove(chatId, out var roomId) && Room.Rooms.TryGetValue(roomId, out var room))
            {
                if (room.HostId == chatId)
                {
                    // Хост пытается покинуть комнату
                    room.Participants = new ConcurrentBag<long>(room.Participants.Where(id => id != chatId));

                    // Удалим комнату, если хост покидает её
                    foreach (var participant in room.Participants)
                    {
                        Room.UserRoomMap.TryRemove(participant, out _);
                        await botClient.SendTextMessageAsync(participant, "Комната была удалена хостом.", cancellationToken: cancellationToken);
                    }

                    Room.Rooms.TryRemove(roomId, out _);
                    Console.WriteLine($"[LOG] Комната {roomId} была удалена хостом {userName}.");

                    await botClient.SendTextMessageAsync(chatId, "Вы покинули комнату, и комната была удалена.", cancellationToken: cancellationToken);
                }
                else
                {
                    // Участник покидает комнату
                    room.Participants = new ConcurrentBag<long>(room.Participants.Where(id => id != chatId));
                    await Notifier.NotifyParticipants(botClient, room, $"@{userName} покинул комнату.", cancellationToken);
                    Console.WriteLine($"[LOG] Пользователь {userName} покинул комнату {roomId}.");

                    await botClient.SendTextMessageAsync(chatId, "Вы покинули комнату.", cancellationToken: cancellationToken);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
            }
        }

        public static async Task DeleteCommand(ITelegramBotClient botClient, long chatId, string userName, CancellationToken cancellationToken)
        {
            if (Room.UserRoomMap.TryGetValue(chatId, out var roomId) && Room.Rooms.TryGetValue(roomId, out var room) && room.HostId == chatId)
            {
                foreach (var participant in room.Participants)
                {
                    Room.UserRoomMap.TryRemove(participant, out _);
                    await botClient.SendTextMessageAsync(participant, "Комната была удалена хостом.", cancellationToken: cancellationToken);
                }

                Room.Rooms.TryRemove(roomId, out _);
                Console.WriteLine($"[LOG] Комната {roomId} была удалена хостом {userName}.");

                await botClient.SendTextMessageAsync(chatId, "Вы удалили комнату.", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Вы не являетесь хостом комнаты.", cancellationToken: cancellationToken);
            }
        }

        public static async Task StartGameCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (!Room.UserRoomMap.TryGetValue(chatId, out var roomId) || !Room.Rooms.TryGetValue(roomId, out var room))
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

        public static async Task HandleGameModeSelection(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!Room.UserRoomMap.TryGetValue(chatId, out var roomId) || !Room.Rooms.TryGetValue(roomId, out var room))
            {
                await botClient.SendTextMessageAsync(chatId, "Вы не находитесь в комнате.", cancellationToken: cancellationToken);
                return;
            }

            if (!room.GameStarted)
            {
                await botClient.SendTextMessageAsync(chatId, "Игра еще не началась.", cancellationToken: cancellationToken);
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

            await botClient.SendTextMessageAsync(chatId, $"Режим игры установлен на {messageText}.", cancellationToken: cancellationToken);
            await NotifyParticipants(botClient, room, "Введите ваше состояние здоровья.", cancellationToken);

            Console.WriteLine($"[LOG] Режим игры в комнате {roomId} установлен на {messageText}.");
        }

        private static async Task NotifyParticipants(ITelegramBotClient botClient, Room room, string message, CancellationToken cancellationToken)
        {
            var tasks = room.Participants.Select(participant =>
                botClient.SendTextMessageAsync(participant, message, cancellationToken: cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}

