using PKHeX.Core;
using System;
using System.Linq;

namespace SysBot.Pokemon.Discord.Helpers.TradeModule;

/// <summary>
/// Handles IV enforcement with hypertraining support and unified nature/shiny manipulation.
/// </summary>
public static class IVEnforcer
{
    /// <summary>
    /// Applies requested IVs and forces nature/shiny through unified PID manipulation.
    /// This preserves exact IV values while ensuring legality through PID-based nature/shiny control.
    /// </summary>
    /// <param name="pk">The Pokemon to modify</param>
    /// <param name="requestedIVs">Array of requested IVs in PKHeX order (HP/Atk/Def/SpA/SpD/Spe)</param>
    /// <param name="nature">The desired nature</param>
    /// <param name="shinyType">The desired shiny type</param>
    public static void ApplyRequestedIVsAndForceNature(PKM pk, int[] requestedIVs, Nature nature, Shiny shinyType)
    {
        if (requestedIVs == null || requestedIVs.Length != 6)
            return;

        // Clear any existing hypertrain flags before setting IVs
        if (pk is IHyperTrain ht)
            ht.HyperTrainClear();

        // Apply the requested IVs
        pk.SetIVs(requestedIVs);

        // Force nature and shiny through PID manipulation
        NatureEnforcer.ForceNatureAndShiny(pk, nature, shinyType);

        // Refresh the checksum to ensure data integrity
        pk.RefreshChecksum();
    }

    /// <summary>
    /// Applies requested IVs without modifying nature or shiny status.
    /// </summary>
    /// <param name="pk">The Pokemon to modify</param>
    /// <param name="requestedIVs">Array of requested IVs in PKHeX order (HP/Atk/Def/SpA/SpD/Spe)</param>
    public static void ApplyRequestedIVs(PKM pk, int[] requestedIVs)
    {
        if (requestedIVs == null || requestedIVs.Length != 6)
            return;

        // Clear any existing hypertrain flags before setting IVs
        if (pk is IHyperTrain ht)
            ht.HyperTrainClear();

        // Apply the requested IVs
        pk.SetIVs(requestedIVs);

        // Refresh the checksum
        pk.RefreshChecksum();
    }

    /// <summary>
    /// Clears hypertrain flags on a Pokemon.
    /// </summary>
    /// <param name="pk">The Pokemon to modify</param>
    public static void ClearHyperTraining(PKM pk)
    {
        if (pk is IHyperTrain ht)
            ht.HyperTrainClear();
    }

    /// <summary>
    /// Extracts IVs from a Showdown set into a standard array format.
    /// </summary>
    /// <param name="set">The showdown set to extract IVs from</param>
    /// <returns>Array of IVs in PKHeX order, or empty array if not specified</returns>
    public static int[] ExtractIVsFromSet(ShowdownSet set)
    {
        if (set.IVs != null && set.IVs.Count() == 6)
            return set.IVs.ToArray();

        return Array.Empty<int>();
    }
}
