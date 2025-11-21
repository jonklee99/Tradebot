using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HelloModule : ModuleBase<SocketCommandContext>
{
    [Command("hello")]
    [Alias("hi")]
    [Summary("Say hello to the bot and get a response.")]
    public async Task PingAsync()
    {
        var response = SysCordSettings.Settings.HelloResponse;
        var message = string.Format(response, Context.User.Mention);

        var embed = new EmbedBuilder()
            .WithDescription(message)
            .WithColor(Color.Blue)
            .Build();

        await ReplyAsync(embed: embed).ConfigureAwait(false);

        try
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
        }
        catch
        {
            // Bot may not have permission to delete messages
        }
    }
}
