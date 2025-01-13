using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using bunker_tg_bot.Models;

namespace bunker_tg_bot.Utilities
{
    public static class Notifier
    {
        public static async Task NotifyParticipants(ITelegramBotClient botClient, Room room, string message, CancellationToken cancellationToken)
        {
            var tasks = room.Participants.Select(participant =>
                botClient.SendTextMessageAsync(participant, message, cancellationToken: cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}