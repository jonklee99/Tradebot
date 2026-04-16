using System.Threading.Tasks;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord.Commands.Bots
{
    public class TradeQueueResult(bool success)
    {
        public bool Success { get; set; } = success;

        internal static Task<bool> InitializeTradeQueueAsync(DiscordSocketClient client) => Task.FromResult(true);
    }
}
