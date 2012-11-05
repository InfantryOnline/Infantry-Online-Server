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
            //TODO: check to see if we are in a league zone
            //Lock the arena
            Basic.speclock(player, null, "all", bong);

            //Toggle Stat Saving for any attached script
            player._arena._isMatch = !player._arena._isMatch;

            //Let Everyone Know
            player._arena.sendArenaMessage("A league match is starting, Please be patient while the referee sets the match up.");
        }
   

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {

            yield return new HandlerDescriptor(startmatch, "startmatch",
                "Toggles a league match and automatically locks an arena",
                "*startmatch", 
                InfServer.Data.PlayerPermission.Mod, false);
        }
    }
}