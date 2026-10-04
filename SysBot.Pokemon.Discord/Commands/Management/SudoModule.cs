using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class SudoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("banID")]
    [Summary("Bans online user IDs.")]
    [RequireSudo]
    public async Task BanOnlineIDs([Summary("Comma Separated Online IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);

        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        hub.Config.TradeAbuse.BannedIDs.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("bannedIDComment")]
    [Summary("Adds a comment for a banned online user ID.")]
    [RequireSudo]
    public async Task BanOnlineIDs(ulong id, [Remainder] string comment)
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var obj = hub.Config.TradeAbuse.BannedIDs.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Unable to find a user with that online ID ({id}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
    }

    [Command("blacklistId")]
    [Summary("Blacklists Discord user IDs. (Useful if user is not in the server).")]
    [RequireSudo]
    public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("blacklist")]
    [Summary("Blacklists a mentioned Discord user.")]
    [RequireSudo]
    public async Task BlackListUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("blacklistComment")]
    [Summary("Adds a comment for a blacklisted Discord user ID.")]
    [RequireSudo]
    public async Task BlackListUsers(ulong id, [Remainder] string comment)
    {
        var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Unable to find a user with that ID ({id}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
    }

    [Command("forgetUser")]
    [Alias("forget")]
    [Summary("Forgets users that were previously encountered.")]
    [RequireSudo]
    public async Task ForgetPreviousUser([Summary("Comma Separated Online IDs")][Remainder] string content)
    {
        foreach (var ID in GetIDs(content))
        {
            PokeRoutineExecutorBase.PreviousUsers.RemoveAllNID(ID);
            PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllNID(ID);
        }
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("bannedIDSummary")]
    [Alias("printBannedID", "bannedIDPrint")]
    [Summary("Prints the list of banned online IDs.")]
    [RequireSudo]
    public async Task PrintBannedOnlineIDs()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var lines = hub.Config.TradeAbuse.BannedIDs.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [Command("blacklistSummary")]
    [Alias("printBlacklist", "blacklistPrint")]
    [Summary("Prints the list of blacklisted Discord users.")]
    [RequireSudo]
    public async Task PrintBlacklist()
    {
        var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [Command("previousUserSummary")]
    [Alias("prevUsers")]
    [Summary("Prints a list of previously encountered users.")]
    [RequireSudo]
    public async Task PrintPreviousUsers()
    {
        bool found = false;
        var lines = PokeRoutineExecutorBase.PreviousUsers.Summarize().ToList();
        if (lines.Count != 0)
        {
            found = true;
            var msg = "Previous Users:\n" + string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        lines = [.. PokeRoutineExecutorBase.PreviousUsersDistribution.Summarize()];
        if (lines.Count != 0)
        {
            found = true;
            var msg = "Previous Distribution Users:\n" + string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }
        if (!found)
            await ReplyAsync("No previous users found.").ConfigureAwait(false);
    }

    [Command("unbanID")]
    [Summary("Bans online user IDs.")]
    [RequireSudo]
    public async Task UnBanOnlineIDs([Summary("Comma Separated Online IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        hub.Config.TradeAbuse.BannedIDs.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("unBlacklistId")]
    [Summary("Removes Discord user IDs from the blacklist. (Useful if user is not in the server).")]
    [RequireSudo]
    public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("unblacklist")]
    [Summary("Removes a mentioned Discord user from the blacklist.")]
    [RequireSudo]
    public async Task UnBlackListUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("banTrade")]
    [Alias("bant")]
    [Summary("Bans a user from trading with a reason.")]
    [RequireSudo]
    public async Task BanTradeUser(ulong userNID, string? userName = null, [Remainder] string? banReason = null)
    {
        await Context.Message.DeleteAsync();
        var dmChannel = await Context.User.CreateDMChannelAsync();
        try
        {
            // Check if the ban reason is provided
            if (string.IsNullOrWhiteSpace(banReason))
            {
                await dmChannel.SendMessageAsync("No reason was supplied. Please use the command as follows:\n.banTrade {NID} {optional: Name} {Reason}\nExample: .banTrade 123456789 Spamming trades");
                return;
            }

            // Use a default name if none is provided
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "Unknown";
            }

            var me = SysCord<T>.Runner;
            var hub = me.Hub;
            var bannedUser = new RemoteControlAccess
            {
                ID = userNID,
                Name = userName,
                Comment = $"Banned by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}. Reason: {banReason}"
            };

            hub.Config.TradeAbuse.BannedIDs.AddIfNew([bannedUser]);
            await dmChannel.SendMessageAsync($"Done. User {userName} with NID {userNID} has been banned from trading.");
        }
        catch (Exception ex)
        {
            await dmChannel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }

    [Command("clearTradeProfile")]
    [Alias("ctp")]
    [Summary("Clears the trade profile for a mentioned user.")]
    [RequireSudo]
    public async Task ClearTradeProfileMentionAsync([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        if (!users.Any())
        {
            await ReplyAsync("Please mention a user to clear their trade profile.").ConfigureAwait(false);
            return;
        }

        var storage = new TradeCodeStorage();
        var results = new List<string>();
        foreach (var user in users)
        {
            bool success = storage.DeleteTradeCode(user.Id);
            results.Add(success
                ? $"Cleared trade profile for {user.Username}."
                : $"No trade profile found for {user.Username}.");
        }
        await ReplyAsync(string.Join("\n", results)).ConfigureAwait(false);
    }

    [Command("clearTradeProfile")]
    [Alias("ctp")]
    [Summary("Clears the trade profile for a user by their Discord ID.")]
    [RequireSudo]
    public async Task ClearTradeProfileIDAsync(ulong userId)
    {
        var storage = new TradeCodeStorage();
        bool success = storage.DeleteTradeCode(userId);
        await ReplyAsync(success
            ? $"Cleared trade profile for user ID {userId}."
            : $"No trade profile found for user ID {userId}.").ConfigureAwait(false);
    }

    [Command("clear")]
    [Summary("Deletes the bot's own messages in this channel. Optionally specify how many to delete (default: all).")]
    [RequireSudo]
    public async Task ClearBotMessagesAsync([Summary("Number of bot messages to delete")] int count = int.MaxValue)
    {
        try
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"Failed to delete clear command message in #{Context.Channel.Name} ({Context.Guild?.Name ?? "DM"}): {ex.Message}", nameof(SudoModule<T>));
        }

        if (count <= 0)
        {
            await ReplyAsync("Please specify a number greater than 0.").ConfigureAwait(false);
            return;
        }

        var botId = Context.Client.CurrentUser.Id;
        var toDelete = new List<IMessage>();
        var before = Context.Message.Id;

        // Page backwards through the channel history, collecting the bot's messages (newest first).
        while (toDelete.Count < count)
        {
            var batch = (await Context.Channel.GetMessagesAsync(before, Direction.Before, 100).FlattenAsync().ConfigureAwait(false)).ToList();
            if (batch.Count == 0)
                break;

            toDelete.AddRange(batch
                .Where(m => m.Author.Id == botId)
                .OrderByDescending(m => m.Id)
                .Take(count - toDelete.Count));
            before = batch.Min(m => m.Id);
        }

        if (toDelete.Count == 0)
        {
            await ReplyAsync("No bot messages found to clear.").ConfigureAwait(false);
            return;
        }

        // Bulk delete only works for messages younger than 14 days and requires Manage Messages.
        var bulkCutoff = DateTimeOffset.UtcNow.AddDays(-14).AddMinutes(5);
        var canBulk = Context.Channel is ITextChannel textChannel
            && Context.Guild?.CurrentUser.GetPermissions(textChannel).ManageMessages == true;
        var bulk = canBulk ? toDelete.Where(m => m.CreatedAt > bulkCutoff).ToList() : [];
        var single = toDelete.Except(bulk).ToList();

        int deleted = 0;
        if (bulk.Count > 0)
        {
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(bulk).ConfigureAwait(false);
            deleted += bulk.Count;
        }

        foreach (var msg in single)
        {
            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
                deleted++;
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Failed to delete message {msg.Id}: {ex.Message}", nameof(SudoModule<T>));
            }
        }

        var confirmation = await ReplyAsync($"Cleared {deleted} bot message(s).").ConfigureAwait(false);
        await Task.Delay(5_000).ConfigureAwait(false);
        await confirmation.DeleteAsync().ConfigureAwait(false);
    }

    protected static IEnumerable<ulong> GetIDs(string content)
    {
        return content.Split([",", ", ", " "], StringSplitOptions.RemoveEmptyEntries)
            .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
    }

    private RemoteControlAccess GetReference(IUser channel) => new()
    {
        ID = channel.Id,
        Name = channel.Username,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(ulong id) => new()
    {
        ID = id,
        Name = "Manual",
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
