using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script
{
    /// <summary>
    /// Provides a number of generic gametype functions
    /// </summary>
    public partial class ScriptHelpers
    {
        /// <summary>
        /// Scrambles all players across all teams in an arena
        /// </summary>
        /// <param name="arena">arena object of your arena</param>
        /// <param name="numTeams">number of teams to scramble arena across</param>
        /// <param name="alertArena">if set to true, will send arena message "Teams have been scrambled"</param>
        public static void scrambleTeams(Arena arena, int numTeams, bool alertArena)
        {
            Random _rand = new Random();

            List<Player> shuffledPlayers = arena.PublicPlayersInGame.OrderBy(plyr => _rand.Next(0, 500)).ToList();

            for (int i = 0; i < shuffledPlayers.Count; i++)
                arena.PublicTeams.ElementAt(i % numTeams).addPlayer(shuffledPlayers[i]);

            //Notify players of the scramble
            if(alertArena)
                arena.sendArenaMessage("Teams have been scrambled!");
        }

        /// <summary>
        /// Converts a number to its ordinal string representation (1st, 2nd, 3rd, etc...)
        /// </summary>
        public static string toOrdinal(int num)
        {
            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num.ToString() + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num.ToString() + "st";
                case 2:
                    return num.ToString() + "nd";
                case 3:
                    return num.ToString() + "rd";
                default:
                    return num.ToString() + "th";
            }

        }
    }
}
