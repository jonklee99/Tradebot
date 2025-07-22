using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static SysBot.Pokemon.TradeSettings.TradeSettingsCategory;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Link Code trades")]
public class TradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    private static readonly char[] separator = [' '];

    private static readonly char[] separatorArray = [' '];

    private static readonly char[] separatorArray0 = [' '];

    [Command("fixOT")]
    [Alias("fix", "f")]
    [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
    [RequireQueueRole(nameof(DiscordManager.RolesFixOT))]
    public async Task FixAdOT()
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var code = Info.GetRandomTradeCode(userID);
        var trainerName = Context.User.Username;
        var lgcode = Info.GetRandomLGTradeCode();
        var sig = Context.User.GetFavor();

        await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, new T(), PokeRoutineType.FixOT, PokeTradeType.FixOT, Context.User, false, 1, 1, false, false, lgcode).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("fixOT")]
    [Alias("fix", "f")]
    [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
    [RequireQueueRole(nameof(DiscordManager.RolesFixOT))]
    public async Task FixAdOT([Summary("Trade Code")] int code)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var trainerName = Context.User.Username;
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();

        await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, new T(), PokeRoutineType.FixOT, PokeTradeType.FixOT, Context.User, false, 1, 1, false, false, lgcode).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("fixOTList")]
    [Alias("fl", "fq")]
    [Summary("Prints the users in the FixOT queue.")]
    [RequireSudo]
    public async Task GetFixListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.FixOT);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    [Command("dittoTrade")]
    [Alias("dt", "ditto")]
    [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
    public async Task DittoTrade([Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        var code = Info.GetRandomTradeCode(userID);
        await DittoTrade(code, keyword, language, nature).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("dittoTrade")]
    [Alias("dt", "ditto")]
    [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
    public async Task DittoTrade([Summary("Trade Code")] int code, [Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        keyword = keyword.ToLower().Trim();

        if (Enum.TryParse(language, true, out LanguageID lang))
        {
            language = lang.ToString();
        }
        else
        {
            _ = ReplyAndDeleteAsync($"Couldn't recognize language: {language}.", 2, Context.Message);
            return;
        }

        nature = nature.Trim()[..1].ToUpper() + nature.Trim()[1..].ToLower();
        var set = new ShowdownSet($"{keyword}(Ditto)\nLanguage: {language}\nNature: {nature}");
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out var result);
        TradeExtensions<T>.DittoTrade((T)pkm);
        var la = new LegalityAnalysis(pkm);

        if (pkm is not T pk || !la.Valid)
        {
            var reason = result == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
            var imsg = $"Oops! {reason} Here's my best attempt for that Ditto!";
            await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
            return;
        }

        pk.ResetPartyStats();

        // Ad Name Check
        if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
        {
            if (TradeExtensions<T>.HasAdName(pk, out string ad))
            {
                await ReplyAndDeleteAsync("Detected Adname in the Pokémon's name or trainer name, which is not allowed.", 5);
                return;
            }
        }

        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("itemTrade")]
    [Alias("it", "item")]
    [Summary("Makes the bot trade you a Pokémon holding the requested item, or Ditto if stat spread keyword is provided.")]
    public async Task ItemTrade([Remainder] string item)
    {
        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
            return;
        }
        var code = Info.GetRandomTradeCode(userID);
        await ItemTrade(code, item).ConfigureAwait(false);
    }

    [Command("itemTrade")]
    [Alias("it", "item")]
    [Summary("Makes the bot trade you a Pokémon holding the requested item.")]
    public async Task ItemTrade([Summary("Trade Code")] int code, [Remainder] string item)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
            return;
        }

        Species species = Info.Hub.Config.Trade.TradeConfiguration.ItemTradeSpecies == Species.None ? Species.Diglett : Info.Hub.Config.Trade.TradeConfiguration.ItemTradeSpecies;
        var set = new ShowdownSet($"{SpeciesName.GetSpeciesNameGeneration((ushort)species, 2, 8)} @ {item.Trim()}");
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out var result);
        pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;

        if (pkm.HeldItem == 0)
        {
            _ = ReplyAndDeleteAsync($"{Context.User.Username}, the item you entered wasn't recognized.", 2, Context.Message);
            return;
        }

        var la = new LegalityAnalysis(pkm);
        if (pkm is not T pk || !la.Valid)
        {
            var reason = result == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
            var imsg = $"Oops! {reason} Here's my best attempt for that {species}!";
            await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
            return;
        }

        pk.ResetPartyStats();
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific).ConfigureAwait(false);

        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("tradeList")]
    [Alias("tl")]
    [Summary("Prints the users in the trade queues.")]
    [RequireSudo]
    public async Task GetTradeListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    [Command("egg")]
    [Alias("Egg")]
    [Summary("Trades an egg generated from the provided Pokémon name.")]
    public async Task TradeEgg([Remainder] string egg)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        await TradeEggAsync(code, egg).ConfigureAwait(false);
    }

    [Command("egg")]
    [Alias("Egg")]
    [Summary("Trades an egg generated from the provided Pokémon name.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeEggAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2);
                return;
            }
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        _ = Task.Run(async () =>
        {
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                if (pkm == null)
                {
                    var response = await ReplyAsync("Set took too long to legalize.");
                    return;
                }
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                if (pkm is not T pk)
                {
                    _ = ReplyAndDeleteAsync("Oops! I wasn't able to create an egg for that.", 2, Context.Message);
                    return;
                }

                bool versionSpecified = content.Contains(".Version=", StringComparison.OrdinalIgnoreCase);

                if (!versionSpecified)
                {
                    if (pk is PB8 pb8)
                    {
                        pb8.Version = (GameVersion)GameVersion.BD;
                    }
                    else if (pk is PK8 pk8)
                    {
                        pk8.Version = (GameVersion)GameVersion.SW;
                    }
                }

                pk.IsNicknamed = false;
                TradeExtensions<T>.EggTrade(pk, template);
                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User).ConfigureAwait(false);
                _ = DeleteMessagesAfterDelayAsync(null, Context.Message, 2);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                _ = ReplyAndDeleteAsync("An error occurred while processing the request.", 2, Context.Message);
            }
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        });

        // Return immediately to avoid blocking
        await Task.CompletedTask;
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task HideTradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        return HideTradeAsync(code, content);
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task HideTradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<Pictocodes>? lgcode = null;
        var userID = Context.User.Id;

        if (Info.IsUserInQueue(userID))
        {
            var existingTrades = Info.GetIsUserQueued(x => x.UserID == userID);
            foreach (var trade in existingTrades)
            {
                trade.Trade.IsProcessing = false;
            }

            var clearResult = Info.ClearTrade(userID);
            if (clearResult == QueueResultRemove.CurrentlyProcessing || clearResult == QueueResultRemove.NotInQueue)
            {
                _ = ReplyAndDeleteAsync("You already have an existing trade in the queue that cannot be cleared. Please wait until it is processed.", 2);
                return;
            }
        }

        var ignoreAutoOT = content.Contains("OT:") || content.Contains("TID:") || content.Contains("SID:");
        content = ReusableActions.StripCodeBlock(content);
        bool isEgg = TradeExtensions<T>.IsEggCheck(content);

        _ = ShowdownParsing.TryParseAnyLanguage(content, out ShowdownSet? set);

        if (set == null || set.Species == 0)
        {
            await ReplyAsync("Unable to parse Showdown set. Could not identify the Pokémon species.");
            return;
        }

        byte finalLanguage = LanguageHelper.GetFinalLanguage(content, set, (byte)Info.Hub.Config.Legality.GenerateLanguage, TradeExtensions<T>.DetectShowdownLanguage);
        var template = AutoLegalityWrapper.GetTemplate(set);

        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
            _ = ReplyAndDeleteAsync(msg, 2, Context.Message);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var sav = LanguageHelper.GetTrainerInfoWithLanguage<T>((LanguageID)finalLanguage);
                var pkm = sav.GetLegal(template, out var result);

                if (pkm == null)
                {
                    var response = await ReplyAsync("Set took too long to legalize.");
                    return;
                }

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];

                if (isEgg && pkm is T eggPk)
                {
                    eggPk.IsNicknamed = false;
                    TradeExtensions<T>.EggTrade(eggPk, template);
                    pkm = eggPk;
                    la = new LegalityAnalysis(pkm);
                }
                else
                {
                    pkm.HeldItem = pkm switch
                    {
                        PA8 => (int)HeldItem.None,
                        _ when pkm.HeldItem == 0 && !pkm.IsEgg => (int)SysCord<T>.Runner.Config.Trade.TradeConfiguration.DefaultHeldItem,
                        _ => pkm.HeldItem
                    };

                    if (pkm is PB7)
                    {
                        lgcode = GenerateRandomPictocodes(3);
                        if (pkm.Species == (int)Species.Mew && pkm.IsShiny)
                        {
                            await ReplyAsync("Mew can **not** be Shiny in LGPE. PoGo Mew does not transfer and Pokeball Plus Mew is shiny locked.");
                            return;
                        }
                    }
                }

                if (pkm is not T pk || !la.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                                 result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                                 $"I wasn't able to create a {spec} from that set.";

                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Trade Creation Failed.")
                        .WithColor(Color.Red)
                        .AddField("Status", $"Failed to create {spec}.")
                        .AddField("Reason", reason);

                    if (result == "Failed")
                    {
                        var legalizationHint = AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm);
                        if (legalizationHint.Contains("Requested shiny value (ShinyType."))
                        {
                            legalizationHint = $"{spec} **cannot** be shiny. Please try again.";
                        }

                        if (!string.IsNullOrEmpty(legalizationHint))
                        {
                            embedBuilder.AddField("Hint", legalizationHint);
                        }
                    }

                    string userMention = Context.User.Mention;
                    string messageContent = $"{userMention}, here's the report for your request:";
                    var message = await Context.Channel.SendMessageAsync(text: messageContent, embed: embedBuilder.Build()).ConfigureAwait(false);
                    _ = DeleteMessagesAfterDelayAsync(message, Context.Message, 30);
                    return;
                }

                if (pk.WasEgg)
                    pk.EggMetDate = pk.MetDate;

                pk.Language = finalLanguage;

                if (!set.Nickname.Equals(pk.Nickname) && string.IsNullOrEmpty(set.Nickname))
                {
                    pk.ClearNickname();
                }

                pk.ResetPartyStats();

                if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
                {
                    if (TradeExtensions<T>.HasAdName(pk, out string ad))
                    {
                        await ReplyAndDeleteAsync("Detected Adname in the Pokémon's name or trainer name, which is not allowed.", 5);
                        return;
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, isBatchTrade: false, batchTradeNumber: 1, totalBatchTrades: 1, true, false, lgcode: lgcode, ignoreAutoOT: ignoreAutoOT, setEdited: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                _ = ReplyAndDeleteAsync(msg, 2, Context.Message);
            }
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 0);
            }
        });

        await Task.CompletedTask;
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you the provided Pokémon file without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task HideTradeAsyncAttach(
            [Summary("Trade Code")] int code,
            [Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var sig = Context.User.GetFavor();
        return HideTradeAsyncAttach(code, sig, Context.User, ignoreAutoOT: ignoreAutoOT);
    }

    [Command("hidetrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you the attached file without showing the trade embed details.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    private async Task HideTradeAsyncAttach([Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        var sig = Context.User.GetFavor();
        await HideTradeAsyncAttach(code, sig, Context.User, ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        return TradeAsync(code, content);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<Pictocodes>? lgcode = null;

        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            var existingTrades = Info.GetIsUserQueued(x => x.UserID == userID);
            foreach (var trade in existingTrades)
            {
                trade.Trade.IsProcessing = false;
            }

            var clearResult = Info.ClearTrade(userID);
            if (clearResult == QueueResultRemove.CurrentlyProcessing || clearResult == QueueResultRemove.NotInQueue)
            {
                _ = ReplyAndDeleteAsync("You already have an existing trade in the queue that cannot be cleared. Please wait until it is processed.", 2);
                return;
            }
        }

        var ignoreAutoOT = content.Contains("OT:") || content.Contains("TID:") || content.Contains("SID:");
        content = ReusableActions.StripCodeBlock(content);
        bool isEgg = TradeExtensions<T>.IsEggCheck(content);

        _ = ShowdownParsing.TryParseAnyLanguage(content, out ShowdownSet? set);

        if (set == null || set.Species == 0)
        {
            await ReplyAsync("Unable to parse Showdown set. Could not identify the Pokémon species.");
            return;
        }

        byte finalLanguage = LanguageHelper.GetFinalLanguage(content, set, (byte)Info.Hub.Config.Legality.GenerateLanguage, TradeExtensions<T>.DetectShowdownLanguage);
        var template = AutoLegalityWrapper.GetTemplate(set);

        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
            _ = ReplyAndDeleteAsync(msg, 2, Context.Message);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var sav = LanguageHelper.GetTrainerInfoWithLanguage<T>((LanguageID)finalLanguage);
                var pkm = sav.GetLegal(template, out var result);
                if (pkm == null)
                {
                    var response = await ReplyAsync("Showdown Set took too long to legalize.");
                    return;
                }

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];

                if (isEgg && pkm is T eggPk)
                {
                    bool versionSpecified = content.Contains(".Version=", StringComparison.OrdinalIgnoreCase);

                    if (!versionSpecified)
                    {
                        if (eggPk is PB8 pb8)
                        {
                            pb8.Version = (GameVersion)GameVersion.BD;
                        }
                        else if (eggPk is PK8 pk8)
                        {
                            pk8.Version = (GameVersion)GameVersion.SW;
                        }
                    }

                    eggPk.IsNicknamed = false;
                    TradeExtensions<T>.EggTrade(eggPk, template);
                    pkm = eggPk;
                    la = new LegalityAnalysis(pkm);
                }
                else
                {
                    pkm.HeldItem = pkm switch
                    {
                        PA8 => (int)HeldItem.None,
                        _ when pkm.HeldItem == 0 && !pkm.IsEgg => (int)SysCord<T>.Runner.Config.Trade.TradeConfiguration.DefaultHeldItem,
                        _ => pkm.HeldItem
                    };

                    if (pkm is PB7)
                    {
                        lgcode = GenerateRandomPictocodes(3);
                        if (pkm.Species == (int)Species.Mew && pkm.IsShiny)
                        {
                            await ReplyAsync("Mew can **not** be Shiny in LGPE. PoGo Mew does not transfer and Pokeball Plus Mew is shiny locked.");
                            return;
                        }
                    }
                }

                if (pkm is not T pk || !la.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                                 result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                                 $"I wasn't able to create a {spec} from that set.";

                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Trade Creation Failed.")
                        .WithColor(Color.Red)
                        .AddField("Status", $"Failed to create {spec}.")
                        .AddField("Reason", reason);

                    if (result == "Failed")
                    {
                        var legalizationHint = AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm);
                        if (legalizationHint.Contains("Requested shiny value (ShinyType."))
                        {
                            legalizationHint = $"{spec} **cannot** be shiny. Please try again.";
                        }

                        if (!string.IsNullOrEmpty(legalizationHint))
                        {
                            embedBuilder.AddField("Hint", legalizationHint);
                        }
                    }

                    string userMention = Context.User.Mention;
                    string messageContent = $"{userMention}, here's the report for your request:";
                    var message = await Context.Channel.SendMessageAsync(text: messageContent, embed: embedBuilder.Build()).ConfigureAwait(false);
                    _ = DeleteMessagesAfterDelayAsync(message, Context.Message, 30);
                    return;
                }

                if (pk.WasEgg)
                    pk.EggMetDate = pk.MetDate;

                pk.Language = finalLanguage;

                if (!set.Nickname.Equals(pk.Nickname) && string.IsNullOrEmpty(set.Nickname))
                {
                    pk.ClearNickname();
                }

                pk.ResetPartyStats();

                if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
                {
                    if (TradeExtensions<T>.HasAdName(pk, out string ad))
                    {
                        await ReplyAndDeleteAsync("Detected Adname in the Pokémon's name or trainer name, which is not allowed.", 5);
                        return;
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, isBatchTrade: false, batchTradeNumber: 1, totalBatchTrades: 1, lgcode: lgcode, ignoreAutoOT: ignoreAutoOT, setEdited: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                _ = ReplyAndDeleteAsync(msg, 2, null);
            }
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        });

        await Task.CompletedTask;
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you the provided Pokémon file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsyncAttach(
    [Summary("Trade Code")] int code,
    [Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var sig = Context.User.GetFavor();
        return TradeAsyncAttachInternal(code, sig, Context.User, ignoreAutoOT: ignoreAutoOT);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you the attached file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncAttach([Summary("Ignore AutoOT")] bool ignoreAutoOT = false)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        var sig = Context.User.GetFavor();
        await Task.Run(async () =>
        {
            await TradeAsyncAttachInternal(code, sig, Context.User, ignoreAutoOT).ConfigureAwait(false);
        }).ConfigureAwait(false);
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    [Command("batchTrade")]
    [Alias("bt")]
    [Summary("Makes the bot trade multiple Pokémon from the provided list, up to a maximum of 4 trades.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task BatchTradeAsync([Summary("List of Showdown Sets separated by '---'")][Remainder] string content)
    {
        // Check if the user is already in the queue
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            var existingTrades = Info.GetIsUserQueued(x => x.UserID == userID);
            foreach (var trade in existingTrades)
            {
                trade.Trade.IsProcessing = false;
            }

            var clearResult = Info.ClearTrade(userID);
            if (clearResult == QueueResultRemove.CurrentlyProcessing || clearResult == QueueResultRemove.NotInQueue)
            {
                _ = ReplyAndDeleteAsync("You already have an existing trade in the queue that cannot be cleared. Please wait until it is processed.", 2);
                return;
            }
        }

        content = ReusableActions.StripCodeBlock(content);
        var trades = ParseBatchTradeContent(content);
        const int maxTradesAllowed = 4;

        // Check if batch mode is allowed and if the number of trades exceeds the limit
        if (maxTradesAllowed < 1 || trades.Count > maxTradesAllowed)
        {
            _ = ReplyAndDeleteAsync($"You can only process up to {maxTradesAllowed} trades at a time. Please reduce the number of trades in your batch.", 5, Context.Message);
            _ = DeleteMessagesAfterDelayAsync(null, Context.Message, 2);
            return;
        }

        var batchTradeCode = Info.GetRandomTradeCode(userID);

        // Process all trades and collect results
        var batchPokemonList = new List<T>();
        var errors = new List<BatchTradeError>();

        for (int i = 0; i < trades.Count; i++)
        {
            var trade = trades[i];
            var (pk, error, set, legalizationHint) = await ProcessSingleTradeForBatch(trade);

            if (pk != null)
            {
                batchPokemonList.Add(pk);
            }
            else
            {
                var speciesName = set != null && set.Species > 0
                    ? GameInfo.Strings.Species[set.Species]
                    : "Unknown";

                errors.Add(new BatchTradeError
                {
                    TradeNumber = i + 1,
                    SpeciesName = speciesName,
                    ErrorMessage = error ?? "Unknown error",
                    LegalizationHint = legalizationHint,
                    ShowdownSet = set != null ? string.Join("\n", set.GetSetLines()) : trade
                });
            }
        }

        // If any trades failed, reject the entire batch with detailed error information
        if (errors.Count > 0)
        {
            var errorMessage = BuildDetailedBatchErrorMessage(errors, trades.Count);

            // Create an embed for better formatting in Discord
            var embed = new EmbedBuilder()
                .WithTitle("❌ Batch Trade Validation Failed")
                .WithColor(Color.Red)
                .WithDescription($"{errors.Count} out of {trades.Count} Pokémon could not be processed.")
                .WithFooter("Please fix the invalid sets and try again.");

            // Add each error as a field in the embed
            foreach (var error in errors)
            {
                var fieldValue = $"**Error:** {error.ErrorMessage}";
                if (!string.IsNullOrEmpty(error.LegalizationHint))
                {
                    fieldValue += $"\n💡 **Hint:** {error.LegalizationHint}";
                }

                // Add first few lines of the showdown set for context
                if (!string.IsNullOrEmpty(error.ShowdownSet))
                {
                    var lines = error.ShowdownSet.Split('\n').Take(2);
                    fieldValue += $"\n**Set:** {string.Join(" | ", lines)}...";
                }

                // Discord embed fields have a 1024 character limit
                if (fieldValue.Length > 1024)
                {
                    fieldValue = fieldValue.Substring(0, 1021) + "...";
                }

                embed.AddField($"Trade #{error.TradeNumber} - {error.SpeciesName}", fieldValue);
            }

            var replyMessage = await ReplyAsync(embed: embed.Build());
            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 20);
            return;
        }

        // All trades are valid, proceed to queue
        if (batchPokemonList.Count > 0)
        {
            await ProcessBatchContainer(batchPokemonList, batchTradeCode, trades.Count);
        }

        // Final cleanup
        if (Context.Message is IUserMessage userMessage)
        {
            _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    private static Task<(T? Pokemon, string? Error, ShowdownSet? Set, string? LegalizationHint)> ProcessSingleTradeForBatch(string tradeContent)
    {
        tradeContent = ReusableActions.StripCodeBlock(tradeContent);
        var ignoreAutoOT = tradeContent.Contains("OT:") || tradeContent.Contains("TID:") || tradeContent.Contains("SID:");
        bool isEgg = TradeExtensions<T>.IsEggCheck(tradeContent);

        _ = ShowdownParsing.TryParseAnyLanguage(tradeContent, out ShowdownSet? set);

        if (set == null || set.Species == 0)
            return Task.FromResult<(T?, string?, ShowdownSet?, string?)>((null, "Unable to parse Showdown set. Could not identify the Pokémon species.", set, null));

        byte finalLanguage = LanguageHelper.GetFinalLanguage(tradeContent, set, (byte)Info.Hub.Config.Legality.GenerateLanguage, TradeExtensions<T>.DetectShowdownLanguage);
        var template = AutoLegalityWrapper.GetTemplate(set);

        var sav = LanguageHelper.GetTrainerInfoWithLanguage<T>((LanguageID)finalLanguage);
        var pkm = sav.GetLegal(template, out var result);
        if (pkm == null)
        {
            var spec = GameInfo.Strings.Species[template.Species];
            var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                         result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                         $"I wasn't able to create a {spec} from that set.";
            return Task.FromResult<(T?, string?, ShowdownSet?, string?)>((null, reason, set, null));
        }

        var la = new LegalityAnalysis(pkm);

        // Handle eggs similar to regular trade commands
        if (isEgg && pkm is T eggPk)
        {
            bool versionSpecified = tradeContent.Contains(".Version=", StringComparison.OrdinalIgnoreCase);

            if (!versionSpecified)
            {
                if (eggPk is PB8 pb8)
                {
                    pb8.Version = (GameVersion)GameVersion.BD;
                }
                else if (eggPk is PK8 pk8)
                {
                    pk8.Version = (GameVersion)GameVersion.SW;
                }
            }

            eggPk.IsNicknamed = false;
            TradeExtensions<T>.EggTrade(eggPk, template);
            pkm = eggPk;
            la = new LegalityAnalysis(pkm);
        }

        if (pkm is not T pk || !la.Valid)
        {
            var spec = GameInfo.Strings.Species[template.Species];
            var reason = result == "Timeout" ? $"That {spec} set took too long to generate." :
                         result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." :
                         $"I wasn't able to create a {spec} from that set.";

            string? legalizationHint = null;
            if (result == "Failed")
            {
                legalizationHint = AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm);
                if (legalizationHint.Contains("Requested shiny value (ShinyType."))
                {
                    legalizationHint = $"{spec} cannot be shiny. Please try again.";
                }
            }

            return Task.FromResult<(T?, string?, ShowdownSet?, string?)>((null, reason, set, legalizationHint));
        }

        // Apply standard processing
        if (pk is PA8)
            pk.HeldItem = (int)HeldItem.None;
        else if (pk.HeldItem == 0 && !pk.IsEgg)
            pk.HeldItem = (int)SysCord<T>.Runner.Config.Trade.TradeConfiguration.DefaultHeldItem;

        if (pk.WasEgg)
            pk.EggMetDate = pk.MetDate;

        pk.Language = finalLanguage;
        if (!set.Nickname.Equals(pk.Nickname) && string.IsNullOrEmpty(set.Nickname))
            pk.ClearNickname();

        pk.ResetPartyStats();

        // Check for spam/ad names
        if (Info.Hub.Config.Trade.TradeConfiguration.EnableSpamCheck)
        {
            if (TradeExtensions<T>.HasAdName(pk, out string ad))
            {
                return Task.FromResult<(T?, string?, ShowdownSet?, string?)>((null, "Detected Adname in the Pokémon's name or trainer name, which is not allowed.", set, null));
            }
        }

        return Task.FromResult<(T?, string?, ShowdownSet?, string?)>((pk, null, set, null));
    }

    private static string BuildDetailedBatchErrorMessage(List<BatchTradeError> errors, int totalTrades)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**Batch Trade Validation Failed**");
        sb.AppendLine($"❌ {errors.Count} out of {totalTrades} Pokémon could not be processed.\n");

        foreach (var error in errors)
        {
            sb.AppendLine($"**Trade #{error.TradeNumber} - {error.SpeciesName}**");
            sb.AppendLine($"Error: {error.ErrorMessage}");

            if (!string.IsNullOrEmpty(error.LegalizationHint))
            {
                sb.AppendLine($"💡 Hint: {error.LegalizationHint}");
            }

            // Optionally include a preview of the showdown set that failed
            if (!string.IsNullOrEmpty(error.ShowdownSet))
            {
                var lines = error.ShowdownSet.Split('\n').Take(3);
                sb.AppendLine($"Set Preview: {string.Join(" | ", lines)}...");
            }

            sb.AppendLine(); // Empty line between errors
        }

        sb.AppendLine("**Please fix the invalid sets and try again.**");
        return sb.ToString();
    }

    private class BatchTradeError
    {
        public int TradeNumber { get; set; }
        public string SpeciesName { get; set; } = "Unknown";
        public string ErrorMessage { get; set; } = "Unknown error";
        public string? LegalizationHint { get; set; }
        public string ShowdownSet { get; set; } = "";
    }

    private async Task ProcessBatchContainer(List<T> batchPokemonList, int batchTradeCode, int totalTrades)
    {
        var userID = Context.User.Id;
        var code = batchTradeCode;
        var sig = Context.User.GetFavor();
        var firstPokemon = batchPokemonList[0];

        // Create a single detail with all batch trades
        await QueueHelper<T>.AddBatchContainerToQueueAsync(Context, code, Context.User.Username, firstPokemon, batchPokemonList, sig, Context.User, totalTrades).ConfigureAwait(false);
    }

    private static List<string> ParseBatchTradeContent(string content)
    {
        var delimiters = new[] { "---", "—-" }; // Includes both three hyphens and an em dash followed by a hyphen
        return [.. content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Select(trade => trade.Trim())];
    }

    [Command("listevents")]
    [Alias("le")]
    [Summary("Lists available event files, filtered by a specific letter or substring, and sends the list via DM.")]
    public async Task ListEventsAsync([Remainder] string args = "")
    {
        const int itemsPerPage = 20; // Number of items per page
        var eventsFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.EventsFolder;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        // Check if the events folder path is not set or empty
        if (string.IsNullOrEmpty(eventsFolderPath))
        {
            _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
            return;
        }

        // Parsing the arguments to separate filter and page number
        string filter = "";
        int page = 1;
        var parts = args.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0)
        {
            // Check if the last part is a number (page number)
            if (int.TryParse(parts.Last(), out int parsedPage))
            {
                page = parsedPage;
                filter = string.Join(" ", parts.Take(parts.Length - 1));
            }
            else
            {
                filter = string.Join(" ", parts);
            }
        }

        var allEventFiles = Directory.GetFiles(eventsFolderPath)
                                     .Select(Path.GetFileNameWithoutExtension)
                                     .OrderBy(file => file)
                                     .ToList();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var filteredEventFiles = allEventFiles
                                 .Where(file => string.IsNullOrWhiteSpace(filter) || file.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                 .ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        IUserMessage replyMessage;

        // Check if there are no files matching the filter
        if (!filteredEventFiles.Any())
        {
            replyMessage = await ReplyAsync($"No events found matching the filter '{filter}'.");
            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
        else
        {
            var pageCount = (int)Math.Ceiling(filteredEventFiles.Count / (double)itemsPerPage);
            page = Math.Clamp(page, 1, pageCount); // Ensure page number is within valid range

            var pageItems = filteredEventFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

            var embed = new EmbedBuilder()
                .WithTitle($"Available Events - Filter: '{filter}'")
                .WithDescription($"Page {page} of {pageCount}")
                .WithColor(Color.Blue);

            foreach (var item in pageItems)
            {
                var index = allEventFiles.IndexOf(item) + 1; // Get the index from the original list
                embed.AddField($"{index}. {item}", $"Use `{botPrefix}er {index}` to request this event.");
            }

            if (Context.User is IUser user)
            {
                try
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: embed.Build());
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I've sent you a DM with the list of events.");
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                {
                    // This exception is thrown when the bot cannot send DMs to the user
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.");
                }
            }
            else
            {
                replyMessage = await ReplyAsync("**Error**: Unable to send a DM. Please check your **Server Privacy Settings**.");
            }

            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
    }

    [Command("eventrequest")]
    [Alias("er")]
    [Summary("Downloads event attachments from the specified EventsFolder and adds to trade queue.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task EventRequestAsync(int index)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2, Context.Message);
            return;
        }

        try
        {
            var eventsFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.EventsFolder;
            var eventFiles = Directory.GetFiles(eventsFolderPath)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToList();

            // Check if the events folder path is not set or empty
            if (string.IsNullOrEmpty(eventsFolderPath))
            {
                _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
                return;
            }

            if (index < 1 || index > eventFiles.Count)
            {
                _ = ReplyAndDeleteAsync("Invalid event index. Please use a valid event number from the `.le` command.", 2, Context.Message);
                return;
            }

            var selectedFile = eventFiles[index - 1]; // Adjust for zero-based indexing
#pragma warning disable CS8604 // Possible null reference argument.
            var fileData = await File.ReadAllBytesAsync(Path.Combine(eventsFolderPath, selectedFile));
#pragma warning restore CS8604 // Possible null reference argument.
            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = GetRequest(download);
            if (pk == null)
            {
                _ = ReplyAndDeleteAsync("Failed to convert event file to the required PKM type.", 2, Context.Message);
                return;
            }

            var code = Info.GetRandomTradeCode(userID);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();

            await ReplyAsync("Event request added to queue.").ConfigureAwait(false);
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode: lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _ = ReplyAndDeleteAsync($"An error occurred: {ex.Message}", 2, Context.Message);
        }
        finally
        {
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        }
    }

    [Command("battlereadylist")]
    [Alias("brl")]
    [Summary("Lists available battle-ready files, filtered by a specific letter or substring, and sends the list via DM.")]
    public async Task BattleReadyListAsync([Remainder] string args = "")
    {
        const int itemsPerPage = 20; // Number of items per page
        var battleReadyFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.BattleReadyPKMFolder;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        // Check if the battleready folder path is not set or empty
        if (string.IsNullOrEmpty(battleReadyFolderPath))
        {
            _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
            return;
        }

        // Parsing the arguments to separate filter and page number
        string filter = "";
        int page = 1;
        var parts = args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0)
        {
            // Check if the last part is a number (page number)
            if (int.TryParse(parts.Last(), out int parsedPage))
            {
                page = parsedPage;
                filter = string.Join(" ", parts.Take(parts.Length - 1));
            }
            else
            {
                filter = string.Join(" ", parts);
            }
        }

        var allBattleReadyFiles = Directory.GetFiles(battleReadyFolderPath)
                                           .Select(Path.GetFileNameWithoutExtension)
                                           .OrderBy(file => file)
                                           .ToList();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var filteredBattleReadyFiles = allBattleReadyFiles
                                       .Where(file => string.IsNullOrWhiteSpace(filter) || file.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                       .ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        IUserMessage replyMessage;

        // Check if there are no files matching the filter
        if (!filteredBattleReadyFiles.Any())
        {
            replyMessage = await ReplyAsync($"No battle-ready files found matching the filter '{filter}'.");
            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
        else
        {
            var pageCount = (int)Math.Ceiling(filteredBattleReadyFiles.Count / (double)itemsPerPage);
            page = Math.Clamp(page, 1, pageCount); // Ensure page number is within valid range

            var pageItems = filteredBattleReadyFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

            var embed = new EmbedBuilder()
                .WithTitle($"Available Battle-Ready Files - Filter: '{filter}'")
                .WithDescription($"Page {page} of {pageCount}")
                .WithColor(Color.Blue);

            foreach (var item in pageItems)
            {
                var index = allBattleReadyFiles.IndexOf(item) + 1; // Get the index from the original list
                embed.AddField($"{index}. {item}", $"Use `{botPrefix}brr {index}` to request this battle-ready file.");
            }

            if (Context.User is IUser user)
            {
                try
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: embed.Build());
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I've sent you a DM with the list of battle-ready files.");
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                {
                    // This exception is thrown when the bot cannot send DMs to the user
                    replyMessage = await ReplyAsync($"{Context.User.Mention}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.");
                }
            }
            else
            {
                replyMessage = await ReplyAsync("**Error**: Unable to send a DM. Please check your **Server Privacy Settings**.");
            }

            _ = DeleteMessagesAfterDelayAsync(replyMessage, Context.Message, 10);
        }
    }

    [Command("battlereadyrequest")]
    [Alias("brr", "br")]
    [Summary("Downloads battle-ready attachments from the specified folder and adds to trade queue.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task BattleReadyRequestAsync(int index)
    {
        var userID = Context.User.Id;

        // Check if the user is already in the queue
        if (Info.IsUserInQueue(userID))
        {
            _ = ReplyAndDeleteAsync("You already have an existing trade in the queue. Please wait until it is processed.", 2, Context.Message);
            return;
        }

        try
        {
            var battleReadyFolderPath = SysCord<T>.Runner.Config.Trade.RequestFolderSettings.BattleReadyPKMFolder;
            var battleReadyFiles = Directory.GetFiles(battleReadyFolderPath)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToList();

            // Check if the battleready folder path is not set or empty
            if (string.IsNullOrEmpty(battleReadyFolderPath))
            {
                _ = ReplyAndDeleteAsync("This bot does not have this feature set up.", 2, Context.Message);
                return;
            }

            if (index < 1 || index > battleReadyFiles.Count)
            {
                _ = ReplyAndDeleteAsync("Invalid battle-ready file index. Please use a valid file number from the `.blr` command.", 2, Context.Message);
                return;
            }

            var selectedFile = battleReadyFiles[index - 1];
#pragma warning disable CS8604 // Possible null reference argument.
            var fileData = await File.ReadAllBytesAsync(Path.Combine(battleReadyFolderPath, selectedFile));
#pragma warning restore CS8604 // Possible null reference argument.
            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = GetRequest(download);
            if (pk == null)
            {
                _ = ReplyAndDeleteAsync("Failed to convert battle-ready file to the required PKM type.", 2, Context.Message);
                return;
            }

            var code = Info.GetRandomTradeCode(userID);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();

            await ReplyAsync("Battle-ready request added to queue.").ConfigureAwait(false);
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode: lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _ = ReplyAndDeleteAsync($"An error occurred: {ex.Message}", 2, Context.Message);
        }
        finally
        {
            if (Context.Message is IUserMessage userMessage)
            {
                _ = DeleteMessagesAfterDelayAsync(userMessage, null, 2);
            }
        }
    }

    [Command("tradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Makes the bot trade the mentioned user the attached file.")]
    [RequireSudo]
    public async Task TradeAsyncAttachUser([Summary("Trade Code")] int code, [Remainder] string _)
    {
        if (Context.Message.MentionedUsers.Count > 1)
        {
            await ReplyAsync("Too many mentions. Queue one user at a time.").ConfigureAwait(false);
            return;
        }

        if (Context.Message.MentionedUsers.Count == 0)
        {
            await ReplyAsync("A user must be mentioned in order to do this.").ConfigureAwait(false);
            return;
        }

        var usr = Context.Message.MentionedUsers.ElementAt(0);
        var sig = usr.GetFavor();
        await Task.Run(async () =>
        {
            await TradeAsyncAttachInternal(code, sig, usr).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [Command("tradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Makes the bot trade the mentioned user the attached file.")]
    [RequireSudo]
    public Task TradeAsyncAttachUser([Remainder] string _)
    {
        var userID = Context.User.Id;
        var code = Info.GetRandomTradeCode(userID);
        return TradeAsyncAttachUser(code, _);
    }

    private async Task TradeAsyncAttachInternal(int code, RequestSignificance sig, SocketUser usr, bool ignoreAutoOT = false)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            await ReplyAsync("No attachment provided!").ConfigureAwait(false);
            return;
        }
        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }
        await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr, ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
    }

    private async Task HideTradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr, bool ignoreAutoOT = false)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            await ReplyAsync("No attachment provided!").ConfigureAwait(false);
            return;
        }

        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }
        await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr, isHiddenTrade: true, ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
    }

    private static T? GetRequest(Download<PKM> dl)
    {
        if (!dl.Success)
            return null;
        return dl.Data switch
        {
            null => null,
            T pk => pk,
            _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
        };
    }

    private async Task AddTradeToQueueAsync(int code, string trainerName, T? pk, RequestSignificance sig, SocketUser usr, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isHiddenTrade = false, bool isMysteryEgg = false, List<Pictocodes>? lgcode = null, PokeTradeType tradeType = PokeTradeType.Specific, bool ignoreAutoOT = false, bool setEdited = false)
    {
        lgcode ??= TradeModule<T>.GenerateRandomPictocodes(3);
        if (pk is not null && !pk.CanBeTraded())
        {
            var reply = await ReplyAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
            await Task.Delay(6000).ConfigureAwait(false); // Delay for 6 seconds
            await reply.DeleteAsync().ConfigureAwait(false);
            return;
        }
        var la = new LegalityAnalysis(pk!);
        if (!la.Valid)
        {
            string responseMessage;
            if (pk?.IsEgg == true)
            {
                string speciesName = SpeciesName.GetSpeciesName(pk.Species, (int)LanguageID.English);
                responseMessage = $"Invalid Showdown Set for the {speciesName} egg. Please review your information and try again.\n```\n{la.Report()}\n```";
            }
            else
            {
                string speciesName = SpeciesName.GetSpeciesName(pk!.Species, (int)LanguageID.English);
                responseMessage = $"{speciesName} attachment is not legal, and cannot be traded!\n\nLegality Report:\n```\n{la.Report()}\n```";
            }
            var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
            await Task.Delay(6000);
            await reply.DeleteAsync().ConfigureAwait(false);
            return;
        }
        bool isNonNative = false;
        if (la.EncounterOriginal.Context != pk?.Context || pk?.GO == true)
        {
            isNonNative = true;
        }
        if (Info.Hub.Config.Legality.DisallowNonNatives && (la.EncounterOriginal.Context != pk?.Context || pk?.GO == true))
        {
            // Allow the owner to prevent trading entities that require a HOME Tracker even if the file has one already.
            string speciesName = SpeciesName.GetSpeciesName(pk!.Species, (int)LanguageID.English);
            await ReplyAsync($"This **{speciesName}** is not native to this game, and cannot be traded!  Trade with the correct bot, then trade to HOME.").ConfigureAwait(false);
            return;
        }
        if (Info.Hub.Config.Legality.DisallowTracked && pk is IHomeTrack { HasTracker: true })
        {
            // Allow the owner to prevent trading entities that already have a HOME Tracker.
            string speciesName = SpeciesName.GetSpeciesName(pk.Species, (int)LanguageID.English);
            await ReplyAsync($"This {speciesName} file is tracked by HOME, and cannot be traded!").ConfigureAwait(false);
            return;
        }
        // handle past gen file requests
        // thanks manu https://github.com/Manu098vm/SysBot.NET/commit/d8c4b65b94f0300096704390cce998940413cc0d
        if (!la.Valid && la.Results.Any(m => m.Identifier is CheckIdentifier.Memory))
        {
            var clone = (T)pk!.Clone();
            clone.HandlingTrainerName = pk.OriginalTrainerName;
            clone.HandlingTrainerGender = pk.OriginalTrainerGender;
            if (clone is PK8 or PA8 or PB8 or PK9)
                ((dynamic)clone).HandlingTrainerLanguage = (byte)pk.Language;
            clone.CurrentHandler = 1;
            la = new LegalityAnalysis(clone);
            if (la.Valid) pk = clone;
        }
        await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk!, PokeRoutineType.LinkTrade, tradeType, usr, isBatchTrade, batchTradeNumber, totalBatchTrades, isHiddenTrade, isMysteryEgg, lgcode, ignoreAutoOT: ignoreAutoOT, setEdited: setEdited, isNonNative: isNonNative).ConfigureAwait(false);
    }

    public static List<Pictocodes> GenerateRandomPictocodes(int count)
    {
        Random rnd = new();
        List<Pictocodes> randomPictocodes = [];
        Array pictocodeValues = Enum.GetValues(typeof(Pictocodes));

        for (int i = 0; i < count; i++)
        {
#pragma warning disable CS8605 // Unboxing a possibly null value.
            Pictocodes randomPictocode = (Pictocodes)pictocodeValues.GetValue(rnd.Next(pictocodeValues.Length));
#pragma warning restore CS8605 // Unboxing a possibly null value.
            randomPictocodes.Add(randomPictocode);
        }

        return randomPictocodes;
    }

    private async Task ReplyAndDeleteAsync(string message, int delaySeconds, IMessage? messageToDelete = null)
    {
        try
        {
            var sentMessage = await ReplyAsync(message).ConfigureAwait(false);
            _ = DeleteMessagesAfterDelayAsync(sentMessage, messageToDelete, delaySeconds);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
        }
    }

    private async Task DeleteMessagesAfterDelayAsync(IMessage? sentMessage, IMessage? messageToDelete, int delaySeconds)
    {
        try
        {
            await Task.Delay(delaySeconds * 1000);

            if (sentMessage != null)
            {
                try
                {
                    await sentMessage.DeleteAsync();
                }
                catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    // Ignore Unknown Message exception
                }
            }

            if (messageToDelete != null)
            {
                try
                {
                    await messageToDelete.DeleteAsync();
                }
                catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    // Ignore Unknown Message exception
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
        }
    }

    [Command("tradeprofile")]
    [Alias("tp")]
    [Summary("Displays the user's trade profile based on their stored trade code information.")]
    public async Task ProfileCommand()
    {
        var userMessage = Context.Message;

        var userId = Context.User.Id.ToString();

        // Path to the tradecodes.json file
        const string TradeCodesFile = "tradecodes.json";

        // Check if the file exists
        if (!File.Exists(TradeCodesFile))
        {
            await Context.Channel.SendMessageAsync("The tradecodes file does not exist.").ConfigureAwait(false);
            await DeleteCommandMessage(userMessage);
            return;
        }

        // Read and parse the JSON file
        string jsonData = await File.ReadAllTextAsync(TradeCodesFile).ConfigureAwait(false);
        var tradeData = JsonConvert.DeserializeObject<Dictionary<string, TradeCodeInfo>>(jsonData);

        if (tradeData == null || !tradeData.ContainsKey(userId))
        {
            await Context.Channel.SendMessageAsync("No trade profile found for you in the database.").ConfigureAwait(false);
            await DeleteCommandMessage(userMessage);
            return;
        }

        // Get user information
        var userInfo = tradeData[userId];

        // Format the trade code
        string formattedTradeCode = $"{userInfo.Code / 10000:D4}-{userInfo.Code % 10000:D4}";

        // Custom image URL for the thumbnail
        const string CustomThumbnailUrl = "https://raw.githubusercontent.com/Joseph11024/Bot-Images/main/Empire/Trainer_Info.png";

        // Validate fields
        string ot = string.IsNullOrWhiteSpace(userInfo.OT) ? "Unknown" : userInfo.OT;
        string tid = userInfo.TID.ToString();
        string sid = userInfo.SID.ToString();
        string tradeCount = userInfo.TradeCount.ToString();

        // Create the embed with a custom thumbnail
        var embed = new EmbedBuilder()
            .WithTitle($"{Context.User.Username}'s Trade Profile")
            .WithColor(Color.Blue)
            .WithThumbnailUrl(CustomThumbnailUrl)
            .AddField("Trade Code", formattedTradeCode, true)
            .AddField("OT", ot, true)
            .AddField("TID", tid, true)
            .AddField("SID", sid, true)
            .AddField("Trade Count", tradeCount, true)
            .WithFooter("Use the bot responsibly!")
            .Build();

        // Try sending the embed to the user's DMs
        try
        {
            var dmChannel = await Context.User.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I've sent your trade profile to your DMs.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Catch any general exceptions (e.g., if the user has DMs disabled)
            await Context.Channel.SendMessageAsync($"{Context.User.Mention}, I couldn't send you a DM. Please check your privacy settings.").ConfigureAwait(false);
            Console.WriteLine($"Failed to send DM to user {Context.User.Id}: {ex.Message}");
        }

        // Delete the user's command message
        await DeleteCommandMessage(userMessage);
    }

    // Helper method to delete a message
    private async Task DeleteCommandMessage(IUserMessage message)
    {
        try
        {
            await message.DeleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete message: {ex.Message}");
        }
    }

    // Helper class to deserialize trade data
    private class TradeCodeInfo
    {
        public int Code { get; set; }
        public string OT { get; set; }
        public int SID { get; set; }
        public int TID { get; set; }
        public int TradeCount { get; set; }
    }

    [Command("medals")]
    [Alias("ml")]
    [Summary("Shows your current trade count and medal status")]
    public async Task ShowMedalsCommand()
    {
        var tradeCodeStorage = new TradeCodeStorage();
        int totalTrades = tradeCodeStorage.GetTradeCount(Context.User.Id);

        if (totalTrades == 0)
        {
            await ReplyAsync($"{Context.User.Username}, you haven't made any trades yet. Start trading to earn your first medal!");
            return;
        }

        int currentMilestone = GetCurrentMilestone(totalTrades);

        var embed = CreateMedalsEmbed(Context.User, currentMilestone, totalTrades);

        try
        {
            // Send the embed to the user's DMs
            var dmChannel = await Context.User.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);

            // Notify in the channel that the message has been sent
            var confirmationMessage = await ReplyAsync($"{Context.User.Mention}, your medals information has been sent to your DMs.").ConfigureAwait(false);

            // Delete the confirmation message after 5 seconds
            await Task.Delay(5000);
            await confirmationMessage.DeleteAsync().ConfigureAwait(false);
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
        {
            // This exception is thrown if the user has DMs disabled
            await ReplyAsync($"{Context.User.Mention}, I couldn't send you a DM. Please check your privacy settings.").ConfigureAwait(false);
        }
    }

    private static int GetCurrentMilestone(int totalTrades)
    {
        int[] milestones = { 700, 650, 600, 550, 500, 450, 400, 350, 300, 250, 200, 150, 100, 50, 1 };
        return milestones.FirstOrDefault(m => totalTrades >= m, 0);
    }

    private static Embed CreateMedalsEmbed(SocketUser user, int milestone, int totalTrades)
    {
        string status = milestone switch
        {
            1 => "Newbie Trainer",
            50 => "Novice Trainer",
            100 => "Pokémon Professor",
            150 => "Pokémon Specialist",
            200 => "Pokémon Champion",
            250 => "Pokémon Hero",
            300 => "Pokémon Elite",
            350 => "Pokémon Trader",
            400 => "Pokémon Sage",
            450 => "Pokémon Legend",
            500 => "Region Master",
            550 => "Trade Master",
            600 => "World Famous",
            650 => "Pokémon Master",
            700 => "Pokémon God",
            _ => "New Trainer"
        };

        string description = $"Total Trades: **{totalTrades}**\n**Current Status:** {status}";

        if (milestone > 0)
        {
            string imageUrl = $"https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/{milestone:D3}.png";
            return new EmbedBuilder()
                .WithTitle($"{user.Username}'s Trading Status")
                .WithColor(new Color(255, 215, 0)) // Gold
                .WithDescription(description)
                .WithThumbnailUrl(imageUrl)
                .Build();
        }
        else
        {
            return new EmbedBuilder()
                .WithTitle($"{user.Username}'s Trading Status")
                .WithColor(new Color(255, 215, 0))
                .WithDescription(description)
                .Build();
        }
    }
}
