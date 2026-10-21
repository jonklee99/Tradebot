using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Touch Trades")]
public class TouchTradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private const int MaxGenerationAttempts = 10;

    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("touchtrade")]
    [Alias("tt")]
    [Summary("Trades you a random Pokémon so the Pokémon you offer can evolve by trade, then sends the evolved Pokémon back.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TouchTradeAsync()
    {
        var code = Info.GetRandomTradeCode(Context.User.Id);
        return TouchTradeAsync(code);
    }

    [Command("touchtrade")]
    [Alias("tt")]
    [Summary("Trades you a random Pokémon so the Pokémon you offer can evolve by trade, then sends the evolved Pokémon back.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TouchTradeAsync([Summary("Trade Code")] int code)
    {
        if (typeof(T) != typeof(PK9))
        {
            await ReplyAsync("Touch Trade is only available for Scarlet/Violet.").ConfigureAwait(false);
            return;
        }

        if (Info.IsUserInQueue(Context.User.Id))
        {
            await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
            return;
        }

        var pk = GenerateRandomPokemon();
        if (pk is null)
        {
            await ReplyAsync("I couldn't generate a Pokémon to trade you. Please try again.").ConfigureAwait(false);
            return;
        }

        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk,
            PokeRoutineType.LinkTrade, PokeTradeType.TouchTrade, Context.User).ConfigureAwait(false);

        _ = DeleteMessageAfterDelayAsync(Context.Message as IUserMessage, 2_000);
    }

    private static T? GenerateRandomPokemon()
    {
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var table = PersonalTable.SV;
        var candidates = Enumerable.Range(1, table.MaxSpeciesID)
            .Select(s => (ushort)s)
            .Where(s => table.IsPresentInGame(s, 0)
                && !SpeciesCategory.IsLegendary(s)
                && !SpeciesCategory.IsSubLegendary(s)
                && !SpeciesCategory.IsMythical(s)
                && !SpeciesCategory.IsParadox(s)
                // Avoid sending something that would itself evolve when the user trades it back.
                && !TradeEvolutions.WillTradeEvolve(s, 0))
            .ToArray();

        for (int i = 0; i < MaxGenerationAttempts; i++)
        {
            var species = candidates[Random.Shared.Next(candidates.Length)];
            var set = new ShowdownSet(SpeciesName.GetSpeciesNameGeneration(species, 2, sav.Generation));
            var template = AutoLegalityWrapper.GetTemplate(set);
            var pkm = sav.GetLegal(template, out _);
            if (pkm is null)
                continue;

            pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            if (pkm is not T pk || !new LegalityAnalysis(pk).Valid)
                continue;

            pk.ResetPartyStats();
            return pk;
        }
        return null;
    }

    private static async Task DeleteMessageAfterDelayAsync(IUserMessage? message, int delayMs)
    {
        await Task.Delay(delayMs).ConfigureAwait(false);
        try
        {
            if (message != null)
                await message.DeleteAsync().ConfigureAwait(false);
        }
        catch (HttpException) { }
    }
}
