using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using bunker_tg_bot.Models;
using bunker_tg_bot.Utilities;
using System.Collections.Concurrent;

namespace bunker_tg_bot.Handlers
{
    public static class UpdateHandler
    {
        private static readonly ConcurrentDictionary<string, string> UserNames = new();

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is { } message && message.Text is { } messageText)
            {
                var chatId = message.Chat.Id;
                var userName = message.From?.Username ?? "Неизвестный";

                if (messageText == "/start")
                {
                    await CommandHandler.StartCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText == "Создать комнату")
                {
                    await CommandHandler.CreateRoomCommand(botClient, chatId, userName, cancellationToken);
                    await CommandHandler.SelectGameModeCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText == "Присоединиться")
                {
                    await CommandHandler.JoinCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText.StartsWith("/join "))
                {
                    await CommandHandler.JoinRoomCommand(botClient, chatId, userName, messageText, cancellationToken);
                }
                else if (messageText == "/members")
                {
                    await CommandHandler.MembersCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText == "/leave")
                {
                    await CommandHandler.LeaveCommand(botClient, chatId, userName, cancellationToken);
                }
                else if (messageText == "/delete")
                {
                    await CommandHandler.DeleteCommand(botClient, chatId, userName, cancellationToken);
                }
                else if (messageText == "Начать игру")
                {
                    await CommandHandler.StartGameCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText == "Сохранить" || messageText == "Изменить данные" || messageText.StartsWith("Изменить "))
                {
                    if (RoomManager.UserRoomMap.TryGetValue(chatId, out var roomId) && RoomManager.Rooms.TryGetValue(roomId, out var room))
                    {
                        await HandlePostCompletionActions(botClient, chatId, room, messageText, cancellationToken);
                    }
                }
                else if (RoomManager.UserRoomMap.TryGetValue(chatId, out var roomId) && RoomManager.Rooms.TryGetValue(roomId, out var room))
                {
                    if (room.GameStarted && room.Participants.Contains(chatId))
                    {
                        await HandleCharacterAttributeInput(botClient, chatId, room, messageText, cancellationToken);
                    }
                    else if (!room.GameStarted && room.HostId == chatId)
                    {
                        await RoomManager.HandleGameModeSelection(botClient, chatId, messageText, cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Неизвестная команда.", cancellationToken: cancellationToken);
                }
            }
        }

        private static async Task HandleCharacterAttributeInput(ITelegramBotClient botClient, long chatId, Room room, string messageText, CancellationToken cancellationToken)
        {
            if (!UserNames.ContainsKey(chatId.ToString()))
            {
                UserNames[chatId.ToString()] = messageText;
                Console.WriteLine($"[LOG] Пользователь @{chatId} ввел имя: {messageText}");
                await botClient.SendTextMessageAsync(chatId, "Введите ваше состояние здоровья.", cancellationToken: cancellationToken);
                return;
            }

            if (!room.UserCharacters.ContainsKey(chatId))
            {
                switch (room.GameMode)
                {
                    case GameMode.Quick:
                        room.UserCharacters[chatId] = new Character();
                        break;
                    case GameMode.Medium:
                        room.UserCharacters[chatId] = new MediumCharacter();
                        break;
                    case GameMode.Detailed:
                        room.UserCharacters[chatId] = new DetailedCharacter();
                        break;
                }
            }

            var character = room.UserCharacters[chatId];

            switch (room.GameMode)
            {
                case GameMode.Quick:
                    await HandleQuickModeInput(botClient, chatId, character, messageText, cancellationToken);
                    break;
                case GameMode.Medium:
                    await HandleMediumModeInput(botClient, chatId, character as MediumCharacter, messageText, cancellationToken);
                    break;
                case GameMode.Detailed:
                    await HandleDetailedModeInput(botClient, chatId, character as DetailedCharacter, messageText, cancellationToken);
                    break;
            }
        }

        private static async Task HandleQuickModeInput(ITelegramBotClient botClient, long chatId, Character character, string messageText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(character.HealthStatus))
            {
                character.HealthStatus = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу работу.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Job))
            {
                character.Job = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш багаж.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Baggage))
            {
                character.Baggage = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваше уникальное знание.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.UniqueKnowledge))
            {
                character.UniqueKnowledge = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш возраст.", cancellationToken: cancellationToken);
                return;
            }
            if (character.Age == 0)
            {
                if (int.TryParse(messageText, out int age))
                {
                    character.Age = age;
                    await botClient.SendTextMessageAsync(chatId, "Введите ваш пол.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите корректный возраст.", cancellationToken: cancellationToken);
                }
                return;
            }
            if (string.IsNullOrEmpty(character.Gender))
            {
                character.Gender = messageText;
                await SendCharacterCard(botClient, chatId, character, cancellationToken);
            }
        }

        private static async Task HandleMediumModeInput(ITelegramBotClient botClient, long chatId, MediumCharacter character, string messageText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(character.HealthStatus))
            {
                character.HealthStatus = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу работу.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Job))
            {
                character.Job = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш багаж.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Baggage))
            {
                character.Baggage = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваше уникальное знание.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.UniqueKnowledge))
            {
                character.UniqueKnowledge = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш возраст.", cancellationToken: cancellationToken);
                return;
            }
            if (character.Age == 0)
            {
                if (int.TryParse(messageText, out int age))
                {
                    character.Age = age;
                    await botClient.SendTextMessageAsync(chatId, "Введите ваш пол.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите корректный возраст.", cancellationToken: cancellationToken);
                }
                return;
            }
            if (string.IsNullOrEmpty(character.Gender))
            {
                character.Gender = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу расу.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Race))
            {
                character.Race = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу фобию.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Phobia))
            {
                character.Phobia = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш характер.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Personality))
            {
                character.Personality = messageText;
                await SendCharacterCard(botClient, chatId, character, cancellationToken);
            }
        }

        private static async Task HandleDetailedModeInput(ITelegramBotClient botClient, long chatId, DetailedCharacter character, string messageText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(character.HealthStatus))
            {
                character.HealthStatus = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу работу.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Job))
            {
                character.Job = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш багаж.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Baggage))
            {
                character.Baggage = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваше уникальное знание.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.UniqueKnowledge))
            {
                character.UniqueKnowledge = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш возраст.", cancellationToken: cancellationToken);
                return;
            }
            if (character.Age == 0)
            {
                if (int.TryParse(messageText, out int age))
                {
                    character.Age = age;
                    await botClient.SendTextMessageAsync(chatId, "Введите ваш пол.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите корректный возраст.", cancellationToken: cancellationToken);
                }
                return;
            }
            if (string.IsNullOrEmpty(character.Gender))
            {
                character.Gender = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу расу.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Race))
            {
                character.Race = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите вашу фобию.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Phobia))
            {
                character.Phobia = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваш характер.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Personality))
            {
                character.Personality = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваше хобби.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Hobby))
            {
                character.Hobby = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите ваше телосложение.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.BodyType))
            {
                character.BodyType = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите факт 1 о вас.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Fact1))
            {
                character.Fact1 = messageText;
                await botClient.SendTextMessageAsync(chatId, "Введите факт 2 о вас.", cancellationToken: cancellationToken);
                return;
            }
            if (string.IsNullOrEmpty(character.Fact2))
            {
                character.Fact2 = messageText;
                await SendCharacterCard(botClient, chatId, character, cancellationToken);
            }
        }

        private static async Task SendCharacterCard(ITelegramBotClient botClient, long chatId, Character character, CancellationToken cancellationToken)
        {
            var characterData = SerializeCharacter(character);
            await botClient.SendTextMessageAsync(chatId, $"Ваша карточка:\n{characterData}", cancellationToken: cancellationToken);

            var buttons = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("Сохранить"),
                new KeyboardButton("Изменить данные")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: buttons, cancellationToken: cancellationToken);
        }

        private static async Task HandlePostCompletionActions(ITelegramBotClient botClient, long chatId, Room room, string messageText, CancellationToken cancellationToken)
        {
            if (messageText == "Сохранить")
            {
                var character = room.UserCharacters[chatId];
                await botClient.SendTextMessageAsync(chatId, "Ваша карточка сохранена.", cancellationToken: cancellationToken);
                await SendCharacterCard(botClient, chatId, character, cancellationToken);

                Console.WriteLine($"[LOG] Пользователь @{botClient.GetChatAsync(chatId).Result.Username} сохранил карточку.\n{SerializeCharacter(character)}");

                if (room.UserCharacters.Values.All(c => c.Gender != null))
                {
                    await botClient.SendTextMessageAsync(chatId, "Все карточки заполнены.", cancellationToken: cancellationToken);
                    Console.WriteLine("[LOG] Все карточки заполнены.");
                }
            }
            else if (messageText == "Изменить данные")
            {
                var character = room.UserCharacters[chatId];
                var buttons = new ReplyKeyboardMarkup(character.GetType().GetProperties()
                    .Select(p => new KeyboardButton(GetRussianPropertyName(p.Name)))
                    .Select(p => new[] { p })
                    .Append(new[] { new KeyboardButton("Сохранить") })
                    .ToArray())
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await botClient.SendTextMessageAsync(chatId, "Выберите характеристику, которую хотите изменить:", replyMarkup: buttons, cancellationToken: cancellationToken);
            }
            else if (messageText.StartsWith("Изменить "))
            {
                var propertyName = messageText.Replace("Изменить ", "");
                room.UserEditState[chatId] = propertyName;
                await botClient.SendTextMessageAsync(chatId, $"Введите новое значение для {propertyName}:", cancellationToken: cancellationToken);
            }
            else if (room.UserEditState.TryGetValue(chatId, out var propertyName))
            {
                await room.SaveChangesAsync(botClient, chatId, messageText, cancellationToken);
            }
        }

        private static string GetRussianPropertyName(string propertyName)
        {
            return propertyName switch
            {
                "HealthStatus" => "Изменить состояние здоровья",
                "Job" => "Изменить работу",
                "Baggage" => "Изменить багаж",
                "UniqueKnowledge" => "Изменить уникальное знание",
                "Age" => "Изменить возраст",
                "Gender" => "Изменить пол",
                "Race" => "Изменить расу",
                "Phobia" => "Изменить фобию",
                "Personality" => "Изменить характер",
                "Hobby" => "Изменить хобби",
                "BodyType" => "Изменить телосложение",
                "Fact1" => "Изменить факт 1",
                "Fact2" => "Изменить факт 2",
                _ => propertyName
            };
        }

        private static string SerializeCharacter(Character character)
        {
            return character switch
            {
                DetailedCharacter dc => $"Здоровье: {dc.HealthStatus}\nРабота: {dc.Job}\nБагаж: {dc.Baggage}\nУникальное знание: {dc.UniqueKnowledge}\nВозраст: {dc.Age}\nПол: {dc.Gender}\nРаса: {dc.Race}\nФобия: {dc.Phobia}\nХарактер: {dc.Personality}\nХобби: {dc.Hobby}\nТелосложение: {dc.BodyType}\nФакт 1: {dc.Fact1}\nФакт 2: {dc.Fact2}",
                MediumCharacter mc => $"Здоровье: {mc.HealthStatus}\nРабота: {mc.Job}\nБагаж: {mc.Baggage}\nУникальное знание: {mc.UniqueKnowledge}\nВозраст: {mc.Age}\nПол: {mc.Gender}\nРаса: {mc.Race}\nФобия: {mc.Phobia}\nХарактер: {mc.Personality}",
                Character c => $"Здоровье: {c.HealthStatus}\nРабота: {c.Job}\nБагаж: {c.Baggage}\nУникальное знание: {c.UniqueKnowledge}\nВозраст: {c.Age}\nПол: {c.Gender}",
                _ => throw new ArgumentException("Unknown character type")
            };
        }
    }
}


