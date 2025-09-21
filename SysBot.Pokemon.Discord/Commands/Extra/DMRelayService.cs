using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord.Helpers
{
    public class DMRelayService
    {
        private readonly DiscordSocketClient _client;
        private readonly ulong _forwardTargetId;
        private readonly DiscordSettings _settings;

        public DMRelayService(DiscordSocketClient client, ulong forwardTargetId, DiscordSettings settings)
        {
            _client = client;
            _forwardTargetId = forwardTargetId;
            _settings = settings;

            if (_forwardTargetId != 0)
                _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage msg)
        {
            if (msg is not SocketUserMessage umsg) return;
            if (umsg.Author.IsBot) return;
            if (umsg.Channel is not SocketDMChannel dm) return;

            // Skip commands
            int argPos = 0;
            bool isCommand = false;
            
            // Check all configured prefixes
            foreach (var prefix in _settings.CommandPrefix)
            {
                if (!string.IsNullOrEmpty(prefix) && umsg.HasStringPrefix(prefix, ref argPos))
                {
                    isCommand = true;
                    break;
                }
            }
            
            if (isCommand || umsg.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            // Build the base forward message
            string forwardContent = $"📩 **DM from {umsg.Author} ({umsg.Author.Id})**:\n{umsg.Content}";

            // Include attachments in the log if they exist
            if (umsg.Attachments.Count > 0)
            {
                forwardContent += "\n\n**Attachments:**";
                foreach (var att in umsg.Attachments)
                {
                    forwardContent += $"\n- [{att.Filename}]({att.Url})"; // Discord Markdown link
                }
            }

            // Try sending to user
            var user = _client.GetUser(_forwardTargetId);
            if (user != null)
            {
                await user.SendMessageAsync(forwardContent);
                return;
            }

            // Try sending to channel
            if (_client.GetChannel(_forwardTargetId) is IMessageChannel channel)
                await channel.SendMessageAsync(forwardContent);
        }
    }
}
