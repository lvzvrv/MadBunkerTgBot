using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using bunker_tg_bot.Models;
using System.Collections.Concurrent;
using Telegram.Bot.Types.ReplyMarkups;

namespace bunker_tg_bot.Handlers
{
    public static class CharacterHandler
    {
        private static readonly ConcurrentDictionary<string, string> UserNames = new();

        public static async Task HandleCharacterAttributeInput(ITelegramBotClient botClient, long chatId, Room room, string messageText, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] HandleCharacterAttributeInput called for chatId: {chatId}, messageText: {messageText}");

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

            if (room.UserEditState.TryGetValue(chatId, out var editField))
            {
                await room.SaveChangesAsync(botClient, chatId, messageText, cancellationToken);
                room.UserEditState.TryRemove(chatId, out _);
                await botClient.SendTextMessageAsync(chatId, "Изменения сохранены. Выберите действие:", cancellationToken: cancellationToken);
                await SendCharacterCard(botClient, chatId, character, cancellationToken);
                return;
            }

            switch (room.GameMode)
            {
                case GameMode.Quick:
                    await HandleQuickModeInput(botClient, chatId, room, character, messageText, cancellationToken);
                    break;
                case GameMode.Medium:
                    await HandleMediumModeInput(botClient, chatId, room, character as MediumCharacter, messageText, cancellationToken);
                    break;
                case GameMode.Detailed:
                    await HandleDetailedModeInput(botClient, chatId, room, character as DetailedCharacter, messageText, cancellationToken);
                    break;
            }
        }

        private static async Task HandleQuickModeInput(ITelegramBotClient botClient, long chatId, Room room, Character character, string messageText, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] HandleQuickModeInput called for chatId: {chatId}, messageText: {messageText}");

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

        private static async Task HandleMediumModeInput(ITelegramBotClient botClient, long chatId, Room room, MediumCharacter character, string messageText, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] HandleMediumModeInput called for chatId: {chatId}, messageText: {messageText}");

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

        private static async Task HandleDetailedModeInput(ITelegramBotClient botClient, long chatId, Room room, DetailedCharacter character, string messageText, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] HandleDetailedModeInput called for chatId: {chatId}, messageText: {messageText}");

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

        private static async Task SendCharacterCard(ITelegramBotClient botClient, long chatId, Character character, CancellationToken cancellationToken, bool showActionMessage = true)
        {
            var characterData = SerializeCharacter(character);
            await botClient.SendTextMessageAsync(chatId, $"Ваша карточка:\n{characterData}", cancellationToken: cancellationToken);

            if (showActionMessage)
            {
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
        }

        public static async Task HandlePostCompletionActions(ITelegramBotClient botClient, long chatId, Room room, string messageText, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] HandlePostCompletionActions called for chatId: {chatId}, messageText: {messageText}");

            if (messageText == "Сохранить")
            {
                Console.WriteLine($"[LOG] Вошли в условие 'Сохранить'");
                var character = room.UserCharacters[chatId];
                character.IsSaved = true; // Устанавливаем поле IsSaved в true
                await botClient.SendTextMessageAsync(chatId, "Ваша карточка сохранена, ждём других игроков", cancellationToken: cancellationToken);
                await SendCharacterCard(botClient, chatId, character, cancellationToken, showActionMessage: false);

                Console.WriteLine($"[LOG] Пользователь @{botClient.GetChatAsync(chatId).Result.Username} сохранил карточку.\n{SerializeCharacter(character)}");

               

                // Вызов метода CheckAllCardsSaved, если все игроки ввели первую характеристику
                if (room.UserCharacters.Values.All(c => !string.IsNullOrEmpty(c.HealthStatus)))
                {
                    Console.WriteLine($"[LOG] Все игроки ввели первую характеристику, вызываем CheckAllCardsSaved");
                    await room.CheckAllCardsSaved(botClient, cancellationToken, chatId);
                }
                

                // Удаление кнопок после сохранения
            }
            else if (messageText == "Изменить данные")
            {
                var character = room.UserCharacters[chatId];
                var buttons = new ReplyKeyboardMarkup(character.GetType().GetProperties()
                    .Select(p => new KeyboardButton($"Изменить {GetRussianPropertyName(p.Name)}"))
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
                var propertyName = GetPropertyNameFromRussian(messageText.Replace("Изменить ", ""));
                room.UserEditState[chatId] = propertyName;
                Console.WriteLine($"[LOG] Set edit state for chatId: {chatId}, propertyName: {propertyName}");
                var russianPropertyName = GetRussianPropertyName(propertyName);
                await botClient.SendTextMessageAsync(chatId, $"Введите новое значение для характеристики: {russianPropertyName}", cancellationToken: cancellationToken);
            }
            else if (room.UserEditState.TryGetValue(chatId, out var propertyName))
            {
                Console.WriteLine($"[LOG] Saving changes for chatId: {chatId}, propertyName: {propertyName}, messageText: {messageText}");
                await room.SaveChangesAsync(botClient, chatId, messageText, cancellationToken);
                await botClient.SendTextMessageAsync(chatId, "Изменения сохранены.", cancellationToken: cancellationToken);
            }
        }


        private static async Task CheckAllCardsSaved(ITelegramBotClient botClient, Room room, CancellationToken cancellationToken)
        {
            if (room.UserCharacters.Values.All(c => c.IsSaved))
            {
                foreach (var participant in room.Participants)
                {
                    await botClient.SendTextMessageAsync(participant, "Карточки всех участников заполнены.", cancellationToken: cancellationToken);
                }
                Console.WriteLine("[LOG] Карточки всех участников заполнены.");
            }
        }

        private static string GetRussianPropertyName(string propertyName)
        {
            return propertyName switch
            {
                "HealthStatus" => "состояние здоровья",
                "Job" => "работу",
                "Baggage" => "багаж",
                "UniqueKnowledge" => "уникальное знание",
                "Age" => "возраст",
                "Gender" => "пол",
                "Race" => "расу",
                "Phobia" => "фобию",
                "Personality" => "характер",
                "Hobby" => "хобби",
                "BodyType" => "телосложение",
                "Fact1" => "факт 1",
                "Fact2" => "факт 2",
                _ => propertyName
            };
        }

        private static string GetPropertyNameFromRussian(string russianPropertyName)
        {
            return russianPropertyName switch
            {
                "состояние здоровья" => "HealthStatus",
                "работу" => "Job",
                "багаж" => "Baggage",
                "уникальное знание" => "UniqueKnowledge",
                "возраст" => "Age",
                "пол" => "Gender",
                "расу" => "Race",
                "фобию" => "Phobia",
                "характер" => "Personality",
                "хобби" => "Hobby",
                "телосложение" => "BodyType",
                "факт 1" => "Fact1",
                "факт 2" => "Fact2",
                _ => russianPropertyName
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
