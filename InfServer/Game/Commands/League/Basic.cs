using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Assets;
using InfServer.Game;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
    /// <summary>
    /// Provides a series of functions for handling mod commands
    /// </summary>
    public class League
    {
        static public void startmatch(Player player, Player recipient, string payload, int bong)
        {
            if (!TryPerformInitialValidation(player, recipient, payload, bong))
            {
                return;
            }

            if (player._arena._isMatch)
            {
                player.sendMessage(-1, "A match has already started, please use stopmatch to end it.");
                return;
            }

            //Lock the arena
            if (!player._arena._bLocked)
                Basic.speclock(player, null, "all", bong);
            //Turn spec quiet on
            if (!player._arena._specQuiet)
                Basic.specquiet(player, null, null, bong);
            //Turn on allowing spectators
            if (player._server._zoneConfig.arena.allowSpectating)
            {
                foreach(Player p in player._arena.Players)
                    if (!p._bAllowSpectator)
                        p._bAllowSpectator = true;
            }

            player._arena._isMatch = true;

            player._arena.gameStart();
            player._arena.sendArenaMessage("Game ON! - Good Luck");
        }

        static public void stopmatch(Player player, Player recipient, string payload, int bong)
        {
            if (!TryPerformInitialValidation(player, recipient, payload, bong))
            {
                return;
            }

            if  (!player._arena._isMatch)
            {
                player.sendMessage(-1, "There is no match currently underway. Use start match to start one.");
                return;
            }

            player._arena._isMatch = false;
            player._arena.sendArenaMessage("League match has ended.");
        }

        static private bool TryPerformInitialValidation(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase))
            {
                player.sendMessage(-1, "This command can only be used in non-public arenas.");
                return false;
            }

            bool isLeagueZone = player._server.Name.Contains("League") || player._server.Name.Contains("USL") || player._server.Name.Contains("CTFPL");
            bool isTestZone = player._server.Name.Contains("TZ") || player._server.Name.Contains("Test"); // League tests if needed...
            if (!isLeagueZone && !isTestZone)
            {
                player.sendMessage(-1, "This command can only be used in league zones.");
                return false;
            }

            return true;
        }
        

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(startmatch, "startmatch",
                "Starts a league match and automatically locks an arena",
                "*startmatch", 
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(stopmatch, "stopmatch",
                "Stops a league match if there is one in progress.",
                "*stopmatch",
                InfServer.Data.PlayerPermission.Mod, true);
        }
    }
}