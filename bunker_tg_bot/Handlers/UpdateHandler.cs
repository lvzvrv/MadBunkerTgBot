using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using bunker_tg_bot.Models;
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

                Console.WriteLine($"[LOG] Received message from chatId: {chatId}, messageText: {messageText}");

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
                    if (Room.UserRoomMap.TryGetValue(chatId, out var roomId) && Room.Rooms.TryGetValue(roomId, out var room))
                    {
                        await CharacterHandler.HandlePostCompletionActions(botClient, chatId, room, messageText, cancellationToken);
                    }
                }
                else if (Room.UserRoomMap.TryGetValue(chatId, out var roomId) && Room.Rooms.TryGetValue(roomId, out var room))
                {
                    if (room.GameStarted && room.Participants.Contains(chatId))
                    {
                        // Проверка, если участник отвечает на вопрос
                        if (QuestionHandler.UserQuestions.ContainsKey(chatId))
                        {
                            await QuestionHandler.HandleAnswerAsync(botClient, chatId, messageText, cancellationToken);
                        }
                        else
                        {
                            await CharacterHandler.HandleCharacterAttributeInput(botClient, chatId, room, messageText, cancellationToken);
                        }
                    }
                    else if (!room.GameStarted && room.HostId == chatId)
                    {
                        await Room.HandleGameModeSelection(botClient, chatId, messageText, cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Неизвестная команда.", cancellationToken: cancellationToken);
                }
            }
            else if (update.CallbackQuery != null)
            {
                var callbackQuery = update.CallbackQuery;
                if (callbackQuery.Data == "skip_story" || callbackQuery.Data == "start_story")
                {
                    await HandleCallbackQuery(botClient, callbackQuery, cancellationToken);
                }
            }
        }

        private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (Room.UserRoomMap.TryGetValue(callbackQuery.From.Id, out var roomId) && Room.Rooms.TryGetValue(roomId, out var room))
            {
                await room.HandleCallbackQuery(botClient, callbackQuery, cancellationToken);
            }
        }
    }
}



