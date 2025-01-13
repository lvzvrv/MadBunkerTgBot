using System;
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
                        else
                        {
                            Console.WriteLine($"[LOG] Invalid age value: {message}");
                        }
                        break;
                    case "Gender":
                        character.Gender = message;
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

                UserEditState.TryRemove(chatId, out _);
                Console.WriteLine($"[LOG] Changes saved for chatId: {chatId}, character: {SerializeCharacter(character)}");
                await botClient.SendTextMessageAsync(chatId, "Изменения сохранены.", cancellationToken: cancellationToken);
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
                            detailedCharacterHobby.Hobby = null;
                        }
                        break;
                    case "BodyType":
                        if (character is DetailedCharacter detailedCharacterBodyType)
                        {
                            detailedCharacterBodyType.BodyType = null;
                        }
                        break;
                    case "Fact1":
                        if (character is DetailedCharacter detailedCharacterFact1)
                        {
                            detailedCharacterFact1.Fact1 = null;
                        }
                        break;
                    case "Fact2":
                        if (character is DetailedCharacter detailedCharacterFact2)
                        {
                            detailedCharacterFact2.Fact2 = null;
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

        private Character CreateCharacterByGameMode(GameMode gameMode)
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
                DetailedCharacter dc => $"Здоровье: {dc.HealthStatus}, Работа: {dc.Job}, Багаж: {dc.Baggage}, Уникальное знание: {dc.UniqueKnowledge}, Возраст: {dc.Age}, Пол: {dc.Gender}, Раса: {dc.Race}, Фобия: {dc.Phobia}, Характер: {dc.Personality}, Хобби: {dc.Hobby}, Телосложение: {dc.BodyType}, Факт 1: {dc.Fact1}, Факт 2: {dc.Fact2}",
                MediumCharacter mc => $"Здоровье: {mc.HealthStatus}, Работа: {mc.Job}, Багаж: {mc.Baggage}, Уникальное знание: {mc.UniqueKnowledge}, Возраст: {mc.Age}, Пол: {mc.Gender}, Раса: {mc.Race}, Фобия: {mc.Phobia}, Характер: {mc.Personality}",
                Character c => $"Здоровье: {c.HealthStatus}, Работа: {c.Job}, Багаж: {c.Baggage}, Уникальное знание: {c.UniqueKnowledge}, Возраст: {c.Age}, Пол: {c.Gender}",
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




