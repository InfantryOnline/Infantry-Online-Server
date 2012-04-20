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
        /// <param name="alertArena">if set to true, will send arena message "Teams have been scrambled"</param>
        static public void scrambleTeams(Arena arena, bool alertArena)
        {
            Random _rand = new Random();

            //Shuffle the players up randomly into a new list
            var random = _rand;
            Player[] shuffledPlayers = arena.PublicPlayersInGame.ToArray(); //Arrays ftw
            for (int i = shuffledPlayers.Length - 1; i >= 0; i--)
            {
                int swap = random.Next(i + 1);
                Player tmp = shuffledPlayers[i];
                shuffledPlayers[i] = shuffledPlayers[swap];
                shuffledPlayers[swap] = tmp;
            }

            //Assign the new list of players to teams
            int j = 1;
            int newteam;
            foreach (Player p in shuffledPlayers)
            {
                Math.DivRem(j, arena.PublicTeams.Count(), out newteam);
                newteam += 1; //Add 1 to account for spec
                if (p._team != arena.Teams.ElementAt(newteam))
                    arena.Teams.ElementAt(newteam).addPlayer(p);
                j++;

            }

            //Notify players of the scramble
            if(alertArena)
                arena.sendArenaMessage("Teams have been scrambled!");
        }
    }
}
