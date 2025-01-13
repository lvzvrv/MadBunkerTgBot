using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using bunker_tg_bot.Handlers;
using Telegram.Bot.Types.Enums;

namespace bunker_tg_bot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string token = "7932579792:AAE-deIyk4-zvC8YoiRNHa3H4rcZdi-rNms";
            var botClient = new TelegramBotClient(token);

            using var cts = new CancellationTokenSource();

            botClient.StartReceiving(
                UpdateHandler.HandleUpdateAsync,
                ErrorHandler.HandleErrorAsync,
                new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
                cts.Token
            );

            Console.WriteLine("Бот запущен. Нажмите Enter для остановки.");
            Console.ReadLine();

            cts.Cancel();
        }
    }
}