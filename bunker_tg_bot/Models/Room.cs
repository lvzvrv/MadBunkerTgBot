using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using bunker_tg_bot.Handlers;

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
            Console.WriteLine($"[LOG] SaveChangesAsync called for chatId: {chatId}, message: {message}");

            if (UserEditState.TryGetValue(chatId, out var editField))
            {
                Console.WriteLine($"[LOG] Editing field: {editField} for chatId: {chatId}");
                if (!UserCharacters.TryGetValue(chatId, out var character))
                {
                    character = CreateCharacterByGameMode(GameMode);
                    UserCharacters[chatId] = character;
                }

                switch (editField)
                {
                    case "UserNameInput":
                        character.UserNameInput = message;
                        UserEditState[chatId] = "HealthStatus";
                        await botClient.SendTextMessageAsync(chatId, "Введите состояние здоровья:", cancellationToken: cancellationToken);
                        break;
                    case "HealthStatus":
                        character.HealthStatus = message;
                        UserEditState[chatId] = "Job";
                        await botClient.SendTextMessageAsync(chatId, "Введите работу:", cancellationToken: cancellationToken);
                        break;
                    case "Job":
                        character.Job = message;
                        UserEditState[chatId] = "Baggage";
                        await botClient.SendTextMessageAsync(chatId, "Введите багаж:", cancellationToken: cancellationToken);
                        break;
                    case "Baggage":
                        character.Baggage = message;
                        UserEditState[chatId] = "UniqueKnowledge";
                        await botClient.SendTextMessageAsync(chatId, "Введите уникальное знание:", cancellationToken: cancellationToken);
                        break;
                    case "UniqueKnowledge":
                        character.UniqueKnowledge = message;
                        UserEditState[chatId] = "Age";
                        await botClient.SendTextMessageAsync(chatId, "Введите возраст:", cancellationToken: cancellationToken);
                        break;
                    case "Age":
                        if (int.TryParse(message, out var age))
                        {
                            character.Age = age;
                            UserEditState[chatId] = "Gender";
                            await botClient.SendTextMessageAsync(chatId, "Введите пол:", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            Console.WriteLine($"[LOG] Invalid age value: {message}");
                            await botClient.SendTextMessageAsync(chatId, "Неверное значение возраста. Попробуйте еще раз:", cancellationToken: cancellationToken);
                        }
                        break;
                    case "Gender":
                        character.Gender = message;
                        UserEditState.TryRemove(chatId, out _);
                        character.IsSaved = true;
                        await botClient.SendTextMessageAsync(chatId, "Персонаж сохранен.", cancellationToken: cancellationToken);
                        break;
                    case "Race":
                        if (character is MediumCharacter mediumCharacter)
                        {
                            mediumCharacter.Race = message;
                        }
                        else if (character is DetailedCharacter detailedCharacter)
                        {
                            detailedCharacter.Race = message;
                        }
                        break;
                    case "Phobia":
                        if (character is MediumCharacter mediumCharacterPhobia)
                        {
                            mediumCharacterPhobia.Phobia = message;
                        }
                        else if (character is DetailedCharacter detailedCharacterPhobia)
                        {
                            detailedCharacterPhobia.Phobia = message;
                        }
                        break;
                    case "Personality":
                        if (character is MediumCharacter mediumCharacterPersonality)
                        {
                            mediumCharacterPersonality.Personality = message;
                        }
                        else if (character is DetailedCharacter detailedCharacterPersonality)
                        {
                            detailedCharacterPersonality.Personality = message;
                        }
                        break;
                    case "Hobby":
                        if (character is DetailedCharacter detailedCharacterHobby)
                        {
                            detailedCharacterHobby.Hobby = message;
                        }
                        break;
                    case "BodyType":
                        if (character is DetailedCharacter detailedCharacterBodyType)
                        {
                            detailedCharacterBodyType.BodyType = message;
                        }
                        break;
                    case "Fact1":
                        if (character is DetailedCharacter detailedCharacterFact1)
                        {
                            detailedCharacterFact1.Fact1 = message;
                        }
                        break;
                    case "Fact2":
                        if (character is DetailedCharacter detailedCharacterFact2)
                        {
                            detailedCharacterFact2.Fact2 = message;
                        }
                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, "Неизвестное поле для редактирования.", cancellationToken: cancellationToken);
                        return;
                }

                Console.WriteLine($"[LOG] Changes saved for chatId: {chatId}, character: {SerializeCharacter(character)}");
            }
            else
            {
                Console.WriteLine($"[LOG] No edit state found for chatId: {chatId}");
                await botClient.SendTextMessageAsync(chatId, "Нет изменений для сохранения.", cancellationToken: cancellationToken);
            }
        }

        public async Task RemoveCharacterAttribute(ITelegramBotClient botClient, long chatId, string attribute, CancellationToken cancellationToken)
        {
            if (UserCharacters.TryGetValue(chatId, out var character))
            {
                switch (attribute)
                {
                    case "HealthStatus":
                        character.HealthStatus = null;
                        break;
                    case "Job":
                        character.Job = null;
                        break;
                    case "Baggage":
                        character.Baggage = null;
                        break;
                    case "UniqueKnowledge":
                        character.UniqueKnowledge = null;
                        break;
                    case "Age":
                        character.Age = 0;
                        break;
                    case "Gender":
                        character.Gender = null;
                        break;
                    case "Race":
                        if (character is MediumCharacter mediumCharacter)
                        {
                            mediumCharacter.Race = null;
                        }
                        else if (character is DetailedCharacter detailedCharacter)
                        {
                            detailedCharacter.Race = null;
                        }
                        break;
                    case "Phobia":
                        if (character is MediumCharacter mediumCharacterPhobia)
                        {
                            mediumCharacterPhobia.Phobia = null;
                        }
                        else if (character is DetailedCharacter detailedCharacterPhobia)
                        {
                            detailedCharacterPhobia.Phobia = null;
                        }
                        break;
                    case "Personality":
                        if (character is MediumCharacter mediumCharacterPersonality)
                        {
                            mediumCharacterPersonality.Personality = null;
                        }
                        else if (character is DetailedCharacter detailedCharacterPersonality)
                        {
                            detailedCharacterPersonality.Personality = null;
                        }
                        break;
                    case "Hobby":
                        if (character is DetailedCharacter detailedCharacterHobby)
                        {
                            detailedCharacterHobby = null;
                        }
                        break;
                    case "BodyType":
                        if (character is DetailedCharacter detailedCharacterBodyType)
                        {
                            detailedCharacterBodyType = null;
                        }
                        break;
                    case "Fact1":
                        if (character is DetailedCharacter detailedCharacterFact1)
                        {
                            detailedCharacterFact1 = null;
                        }
                        break;
                    case "Fact2":
                        if (character is DetailedCharacter detailedCharacterFact2)
                        {
                            detailedCharacterFact2 = null;
                        }
                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, "Неизвестное поле для удаления.", cancellationToken: cancellationToken);
                        return;
                }

                Console.WriteLine($"[LOG] {attribute} removed for chatId: {chatId}");
                await botClient.SendTextMessageAsync(chatId, $"{attribute} удалено. Введите новое значение для {attribute}:", cancellationToken: cancellationToken);
                UserEditState[chatId] = attribute;
            }
            else
            {
                Console.WriteLine($"[LOG] No character found for chatId: {chatId}");
                await botClient.SendTextMessageAsync(chatId, "Персонаж не найден.", cancellationToken: cancellationToken);
            }
        }

        public async Task ResetCharacterAttributes(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (UserCharacters.TryGetValue(chatId, out var character))
            {
                character.UserNameInput = null;
                character.HealthStatus = null;
                character.Job = null;
                character.Baggage = null;
                character.UniqueKnowledge = null;
                character.Age = 0;
                character.Gender = null;

                if (character is MediumCharacter mediumCharacter)
                {
                    mediumCharacter.Race = null;
                    mediumCharacter.Phobia = null;
                    mediumCharacter.Personality = null;
                }

                if (character is DetailedCharacter detailedCharacter)
                {
                    detailedCharacter.Hobby = null;
                    detailedCharacter.BodyType = null;
                    detailedCharacter.Fact1 = null;
                    detailedCharacter.Fact2 = null;
                }

                Console.WriteLine($"[LOG] Attributes reset for chatId: {chatId}");
                
            }
            else
            {
                Console.WriteLine($"[LOG] No character found for chatId: {chatId}");
                
            }
        }

        public Character CreateCharacterByGameMode(GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.Quick => new Character(),
                GameMode.Medium => new MediumCharacter(),
                GameMode.Detailed => new DetailedCharacter(),
                _ => throw new ArgumentException("Unknown game mode")
            };
        }

        private string SerializeCharacter(Character character)
        {
            return character switch
            {
                DetailedCharacter dc => $"Имя: {dc.UserNameInput}\nЗдоровье: {dc.HealthStatus}\nРабота: {dc.Job}\nБагаж: {dc.Baggage}\nУникальное знание: {dc.UniqueKnowledge}\nВозраст: {dc.Age}\nПол: {dc.Gender}\nРаса: {dc.Race}\nФобия: {dc.Phobia}\nХарактер: {dc.Personality}\nХобби: {dc.Hobby}\nТелосложение: {dc.BodyType}\nФакт 1: {dc.Fact1}\nФакт 2: {dc.Fact2}",
                MediumCharacter mc => $"Имя: {mc.UserNameInput}\nЗдоровье: {mc.HealthStatus}\nРабота: {mc.Job}\nБагаж: {mc.Baggage}\nУникальное знание: {mc.UniqueKnowledge}\nВозраст: {mc.Age}\nПол: {mc.Gender}\nРаса: {mc.Race}\nФобия: {mc.Phobia}\nХарактер: {mc.Personality}",
                Character c => $"Имя: {c.UserNameInput}\nЗдоровье: {c.HealthStatus}\nРабота: {c.Job}\nБагаж: {c.Baggage}\nУникальное знание: {c.UniqueKnowledge}\nВозраст: {c.Age}\nПол: {c.Gender}",
                _ => throw new ArgumentException("Unknown character type")
            };
        }

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

            // Создание карточек для всех участников
            foreach (var participant in room.Participants)
            {
                if (!room.UserCharacters.ContainsKey(participant))
                {
                    room.UserCharacters[participant] = room.CreateCharacterByGameMode(room.GameMode);
                }
            }

            Console.WriteLine($"[LOG] Режим игры в комнате {roomId} установлен на {messageText}.");
        }

        private static async Task NotifyParticipants(ITelegramBotClient botClient, Room room, string message, CancellationToken cancellationToken)
        {
            foreach (var participant in room.Participants)
            {
                await botClient.SendTextMessageAsync(participant, message, cancellationToken: cancellationToken);
            }
        }

        public async Task CheckAllCardsSaved(ITelegramBotClient botClient, CancellationToken cancellationToken, ChatId chatId)
        {
            Console.WriteLine("[LOG] Проверка всех карточек на сохранение");

            if (UserCharacters.Values.All(c => c.IsSaved))
            {
                

                Console.WriteLine("[LOG] Все карточки сохранены, начинаем перемешивание");

                await ShuffleCharacterAttributes(botClient, cancellationToken);

                foreach (var participant in Participants)
                {
                    await botClient.SendTextMessageAsync(participant, "Кнопки удалены.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                    await botClient.SendTextMessageAsync(participant, "Карточки всех участников заполнены.", cancellationToken: cancellationToken);
                }
                Console.WriteLine("[LOG] Карточки всех участников заполнены.");

                // Отправка сообщения хосту
                await AskHostForStory(botClient, cancellationToken);
            }
        }

        

        private async Task ShuffleCharacterAttributes(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] Зашли в шафл");

            var healthStatuses = new List<string>();
            var jobs = new List<string>();
            var baggages = new List<string>();
            var uniqueKnowledges = new List<string>();
            var ages = new List<int>();
            var genders = new List<string>();
            var races = new List<string>();
            var phobias = new List<string>();
            var personalities = new List<string>();
            var hobbies = new List<string>();
            var bodyTypes = new List<string>();
            var fact1s = new List<string>();
            var fact2s = new List<string>();

            foreach (var character in UserCharacters.Values)
            {
                healthStatuses.Add(character.HealthStatus);
                jobs.Add(character.Job);
                baggages.Add(character.Baggage);
                uniqueKnowledges.Add(character.UniqueKnowledge);
                ages.Add(character.Age);
                genders.Add(character.Gender);

                if (character is MediumCharacter mediumCharacter)
                {
                    races.Add(mediumCharacter.Race);
                    phobias.Add(mediumCharacter.Phobia);
                    personalities.Add(mediumCharacter.Personality);
                }

                if (character is DetailedCharacter detailedCharacter)
                {
                    hobbies.Add(detailedCharacter.Hobby);
                    bodyTypes.Add(detailedCharacter.BodyType);
                    fact1s.Add(detailedCharacter.Fact1);
                    fact2s.Add(detailedCharacter.Fact2);
                }
            }

            Shuffle(healthStatuses);
            Shuffle(jobs);
            Shuffle(baggages);
            Shuffle(uniqueKnowledges);
            Shuffle(ages);
            Shuffle(genders);
            Shuffle(races);
            Shuffle(phobias);
            Shuffle(personalities);
            Shuffle(hobbies);
            Shuffle(bodyTypes);
            Shuffle(fact1s);
            Shuffle(fact2s);

            int index = 0;
            foreach (var character in UserCharacters.Values)
            {
                character.HealthStatus = healthStatuses[index];
                character.Job = jobs[index];
                character.Baggage = baggages[index];
                character.UniqueKnowledge = uniqueKnowledges[index];
                character.Age = ages[index];
                character.Gender = genders[index];

                if (character is MediumCharacter mediumCharacter)
                {
                    mediumCharacter.Race = races[index];
                    mediumCharacter.Phobia = phobias[index];
                    mediumCharacter.Personality = personalities[index];
                }

                if (character is DetailedCharacter detailedCharacter)
                {
                    detailedCharacter.Hobby = hobbies[index];
                    detailedCharacter.BodyType = bodyTypes[index];
                    detailedCharacter.Fact1 = fact1s[index];
                    detailedCharacter.Fact2 = fact2s[index];
                }

                index++;
            }

            foreach (var character in UserCharacters.Values)
            {
                Console.WriteLine($"[LOG] Updated character: {SerializeCharacter(character)}");
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private async Task AskHostForStory(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var buttons = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Начать историю", "start_story") },
                new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Пропустить", "skip_story") }
            });

            await botClient.SendTextMessageAsync(HostId, "Будете придумывать историю бункера или нет?", replyMarkup: buttons, cancellationToken: cancellationToken);
        }

        public async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[LOG] HandleCallbackQuery called with data: {callbackQuery.Data}");

            if (callbackQuery.Data == "skip_story")
            {
                Console.WriteLine("[LOG] Skip story selected");
                await SendCharacterCardsToAllParticipants(botClient, cancellationToken);
            }
            else if (callbackQuery.Data == "start_story")
            {
                Console.WriteLine("[LOG] Start story selected");
                await QuestionHandler.AssignAndAskQuestionsAsync(botClient, this, cancellationToken);
            }
            // Обработка других callback-запросов, если необходимо
        }

        private async Task SendCharacterCardsToAllParticipants(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            foreach (var participant in Participants)
            {
                if (UserCharacters.TryGetValue(participant, out var character))
                {
                    var characterData = SerializeCharacter(character);
                    await botClient.SendTextMessageAsync(participant, $"Ваша карточка:\n{characterData}", cancellationToken: cancellationToken);
                }
            }
        }
    }
}

