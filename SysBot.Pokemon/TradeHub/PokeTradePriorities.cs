namespace SysBot.Pokemon;

public static class PokeTradePriorities
{
    public const uint Tier1 = 1;

    public const uint Tier2 = 2;

    public const uint Tier3 = 3;

    public const uint Tier4 = 4;

    public const uint TierFree = uint.MaxValue;

    // Base offset for regular (non-sudo) users in the ID-based priority scheme.
    // Sudo users get key = trade.ID (0–49,999).
    // Regular users get key = UserBase + trade.ID (100,000–149,999).
    // This guarantees strict FIFO within each group and sudo always before regular.
    public const uint UserBase = 100_000;
}
