using PKHeX.Core;
using System;

namespace SysBot.Pokemon.Discord.Helpers.TradeModule;

/// <summary>
/// Handles nature enforcement through PID manipulation.
/// </summary>
public static class NatureEnforcer
{
    /// <summary>
    /// Forces a specific nature on a Pokemon by manipulating its PID while preserving shininess.
    /// </summary>
    /// <param name="pk">The Pokemon to modify</param>
    /// <param name="nature">The desired nature</param>
    public static void ForceNature(PKM pk, Nature nature)
    {
        if (pk.Nature == nature)
            return;

        var shinyType = pk.IsShiny ? (pk.ShinyXor == 0 ? Shiny.AlwaysSquare : Shiny.AlwaysStar) : Shiny.Never;

        // Use CommonEdits to set PID with nature and preserve shininess
        CommonEdits.SetShiny(pk, shinyType);

        // Now force the nature by manipulating PID
        var currentPID = pk.PID;
        var targetNatureValue = (byte)nature;
        var currentNatureValue = currentPID % 25;

        if (currentNatureValue == targetNatureValue)
            return;

        // Calculate the PID adjustment needed
        var adjustment = (targetNatureValue - currentNatureValue + 25) % 25;
        var newPID = currentPID - currentNatureValue + targetNatureValue;

        // Ensure we maintain the same shininess
        pk.PID = newPID;

        // If shininess changed, we need to try different PID values
        int attempts = 0;
        while (pk.Nature != nature && attempts < 10000)
        {
            if (shinyType == Shiny.Never && pk.IsShiny)
            {
                pk.PID += 25;
            }
            else if (shinyType != Shiny.Never && !pk.IsShiny)
            {
                pk.PID += 25;
            }
            else if (pk.Nature != nature)
            {
                pk.PID += 25;
            }
            else
            {
                break;
            }
            attempts++;
        }

        pk.Nature = pk.StatNature = nature;
    }

    /// <summary>
    /// Forces a specific nature and shininess through unified PID manipulation.
    /// </summary>
    /// <param name="pk">The Pokemon to modify</param>
    /// <param name="nature">The desired nature</param>
    /// <param name="shinyType">The desired shiny type</param>
    public static void ForceNatureAndShiny(PKM pk, Nature nature, Shiny shinyType)
    {
        // First set the desired shininess
        CommonEdits.SetShiny(pk, shinyType);

        // Then force the nature while maintaining shininess
        ForceNature(pk, nature);
    }
}
