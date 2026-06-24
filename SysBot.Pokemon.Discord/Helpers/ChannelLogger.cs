using Discord;
using Discord.Net;
using Discord.WebSocket;
using SysBot.Base;

using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class ChannelLogger(ulong ChannelID, ISocketMessageChannel Channel) : ILogForwarder
{
    public ulong ChannelID { get; } = ChannelID;

    public string ChannelName => Channel.Name;

    public void Forward(string message, string identity)
    {
        try
        {
            var text = GetMessage(message, identity);
            _ = Channel.SendMessageAsync(text).ContinueWith(t =>
            {
                if (!t.IsFaulted || t.Exception == null)
                    return;

                var inner = t.Exception.InnerException ?? t.Exception;
                string location = GetChannelLocation();

                if (inner is HttpException httpEx && httpEx.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    LogUtil.LogError($"Missing Access sending log message. {location}", identity);
                else
                    LogUtil.LogError($"Failed to send log message. {location} Error: {inner.Message}", identity);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, identity);
        }
    }

    private string GetChannelLocation()
    {
        if (Channel is SocketTextChannel tc)
            return $"Server: {tc.Guild.Name} ({tc.Guild.Id}), Channel: #{tc.Name} ({ChannelID})";
        return $"Channel: #{Channel.Name} ({ChannelID})";
    }

    private static string GetMessage(ReadOnlySpan<char> msg, string identity)
        => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";
}
