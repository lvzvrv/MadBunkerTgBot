using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using bunker_tg_bot.Models;
using System.Collections.Concurrent;

namespace bunker_tg_bot.Handlers
{
    public static class QuestionHandler
    {
        private static readonly List<string> Questions = new List<string>
        {
            "Что произошло?",
            "Какие последствия?",
            "Площадь бункера и сколько нужно в нём просидеть?",
            "Описание бункера?",
            "Местоположение?",
            "Какие есть помещения и их состояние?",
            "Доступные ресурсы?"
        };

        internal static readonly ConcurrentDictionary<long, Queue<string>> UserQuestions = new();
        internal static readonly ConcurrentDictionary<long, Dictionary<string, string>> UserAnswers = new();
        internal static readonly ConcurrentDictionary<long, string> CurrentQuestions = new();
        internal static bool AllAnswersCollected = false;

        public static async Task AssignAndAskQuestionsAsync(ITelegramBotClient botClient, Room room, CancellationToken cancellationToken)
        {
            var shuffledQuestions = Questions.OrderBy(q => Guid.NewGuid()).ToList();
            var shuffledParticipants = room.Participants.OrderBy(p => Guid.NewGuid()).ToList();

            var assignments = new Dictionary<long, Queue<string>>();
            int questionIndex = 0;

            while (questionIndex < shuffledQuestions.Count)
            {
                foreach (var participant in shuffledParticipants)
                {
                    if (questionIndex < shuffledQuestions.Count)
                    {
                        if (!assignments.ContainsKey(participant))
                        {
                            assignments[participant] = new Queue<string>();
                        }
                        assignments[participant].Enqueue(shuffledQuestions[questionIndex]);
                        questionIndex++;
                    }
                }
            }

            foreach (var assignment in assignments)
            {
                var participant = assignment.Key;
                UserQuestions[participant] = assignment.Value;
                UserAnswers[participant] = new Dictionary<string, string>();
                await AskNextQuestion(botClient, participant, cancellationToken);
            }
        }

        private static async Task AskNextQuestion(ITelegramBotClient botClient, long participant, CancellationToken cancellationToken)
        {
            if (UserQuestions.TryGetValue(participant, out var questions) && questions.Any())
            {
                var nextQuestion = questions.Dequeue();
                CurrentQuestions[participant] = nextQuestion;
                await botClient.SendTextMessageAsync(participant, nextQuestion, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(participant, "Вы ответили на все вопросы, ожидайте пока другие закончат.", cancellationToken: cancellationToken);
                await CheckAllAnswersCollected(botClient, cancellationToken);
            }
        }

        public static async Task HandleAnswerAsync(ITelegramBotClient botClient, long participant, string answer, CancellationToken cancellationToken)
        {
            if (AllAnswersCollected)
            {
                return;
            }

            if (CurrentQuestions.TryGetValue(participant, out var currentQuestion) && UserAnswers.TryGetValue(participant, out var answers))
            {
                Console.WriteLine($"[LOG] Saving answer for participant {participant}: {currentQuestion} = {answer}");
                answers[currentQuestion] = answer;
                await AskNextQuestion(botClient, participant, cancellationToken);
            }
        }

        private static async Task CheckAllAnswersCollected(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            Console.WriteLine("[LOG] Checking if all answers are collected");
            foreach (var participant in UserAnswers.Keys)
            {
                Console.WriteLine($"[LOG] Participant {participant} has answered {UserAnswers[participant].Count} questions");
            }

            if (UserQuestions.Keys.All(participant => !UserQuestions[participant].Any()))
            {
                AllAnswersCollected = true;
                Console.WriteLine("[LOG] All answers collected, forming final text");
                var finalText = FormFinalText();
                Console.WriteLine($"[LOG] Final text: {finalText}");
                await SendFinalTextToAllParticipants(botClient, finalText, cancellationToken);
            }
        }

        private static string FormFinalText()
        {
            var finalText = "";
            foreach (var question in Questions)
            {
                var answers = UserAnswers.Values.SelectMany(answers => answers)
                                                .Where(pair => pair.Key == question)
                                                .Select(pair => pair.Value)
                                                .ToList();
                finalText += $"{question}: {string.Join(", ", answers)}\n";
            }
            Console.WriteLine($"[LOG] Formed final text: {finalText}");
            return finalText;
        }

        private static async Task SendFinalTextToAllParticipants(ITelegramBotClient botClient, string finalText, CancellationToken cancellationToken)
        {
            foreach (var participant in UserQuestions.Keys)
            {
                await botClient.SendTextMessageAsync(participant, "Все ответы получены", cancellationToken: cancellationToken);
                await botClient.SendTextMessageAsync(participant, finalText, cancellationToken: cancellationToken);
            }
        }
    }
}











