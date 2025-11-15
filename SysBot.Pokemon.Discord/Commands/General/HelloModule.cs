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
        var str = SysCordSettings.Settings.HelloResponse;
        var imageUrl = string.Format(str, Context.User.Mention);

        var embed = new EmbedBuilder()
            .WithDescription($"Hello {Context.User.Mention}!")
            .WithImageUrl(imageUrl)
            .WithColor(Color.Blue)
            .Build();

        await ReplyAsync(embed: embed).ConfigureAwait(false);
        await Context.Message.DeleteAsync();
    }
}
