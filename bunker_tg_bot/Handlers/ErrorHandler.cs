using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace bunker_tg_bot.Handlers
{
    public static class ErrorHandler
    {
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[ERROR] Произошла ошибка: {exception}");
            return Task.CompletedTask;
        }
    }
}