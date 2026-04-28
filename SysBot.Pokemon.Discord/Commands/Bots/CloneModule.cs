using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Clone trades")]
public class CloneModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public async Task CloneAsync(int code)
    {
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
            return;
        }

        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();

        _ = QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone, Context.User, false, 1, 1, false, false, lgcode: lgcode);

        var confirmationMessage = await ReplyAsync("Processing your clone request...").ConfigureAwait(false);
        _ = DeleteMessagesAfterDelayAsync(Context.Message as IUserMessage, confirmationMessage, 2000);
    }

    [Command("clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public async Task CloneAsync([Summary("Trade Code")][Remainder] string code)
    {
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
            return;
        }

        int tradeCode = Util.ToInt32(code);
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();

        _ = QueueHelper<T>.AddToQueueAsync(Context, tradeCode == 0 ? Info.GetRandomTradeCode(userID) : tradeCode, Context.User.Username, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone, Context.User, false, 1, 1, false, false, lgcode: lgcode);

        var confirmationMessage = await ReplyAsync("Processing your clone request...").ConfigureAwait(false);
        _ = DeleteMessagesAfterDelayAsync(Context.Message as IUserMessage, confirmationMessage, 2000);
    }

    [Command("clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public Task CloneAsync()
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        return CloneAsync(code);
    }

    [Command("cloneList")]
    [Alias("cl", "cq")]
    [Summary("Prints the users in the Clone queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Clone);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    private static async Task DeleteMessagesAfterDelayAsync(IUserMessage? userMessage, IUserMessage? botMessage, int delayMs)
    {
        await Task.Delay(delayMs).ConfigureAwait(false);
        try
        {
            if (userMessage != null)
                await userMessage.DeleteAsync().ConfigureAwait(false);
            if (botMessage != null)
                await botMessage.DeleteAsync().ConfigureAwait(false);
        }
        catch (HttpException) { }
    }
}
