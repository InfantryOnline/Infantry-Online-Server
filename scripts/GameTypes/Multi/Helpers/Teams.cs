using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public static class TeamHelpers
    {

        public static void scrambleTeams(this Arena arena, IEnumerable<Player> unorderedPlayers, List<Team> teams, int maxPerTeam)
        {
            Random _rand = new Random();
            List<Player> players = unorderedPlayers.OrderBy(plyr => _rand.Next(0, 500)).ToList();

            //gets the minimum number of teams we need to fit our players
            int numTeams = players.Count / maxPerTeam + (players.Count % maxPerTeam == 0 ? 0 : 1);

            //adds our players to these teams in team-order
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                teams[i % numTeams].addPlayer(p);
            }

            arena.sendArenaMessage("Teams have been scrambled");
        }

        public static bool inArea(this Player player, int xMin, int yMin, int xMax, int yMax)
        {
            Helpers.ObjectState state = player.getState();
            int px = state.positionX;
            int py = state.positionY;
            return (xMin <= px && px <= xMax && yMin <= py && py <= yMax);
        }

        public static void resetSkills(this Player player)
        {
            for (int i = 0; i < 100; i++) // we gotta remove any class skills that already got
            {
                if (player.findSkill(i) != null)
                    player._skills.Remove(i);
            }
        }

        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        public static bool joinTeam(this Player player, Team team)
        {
            //Sanity checks
            if (player._occupiedVehicle == null)
            {
                Log.write(TLog.Warning, "Attempted to unspectate with no spectator vehicle. {0}", player);
                return false;
            }

            //Make sure our vehicle is a spectator mode vehicle
            if (player._occupiedVehicle._type.Type != VehInfo.Types.Spectator)
            {
                Log.write(TLog.Warning, "Attempted to unspectate with non-spectator vehicle. {0}", player);
                return false;
            }

            //Reset leftover variables
            player._deathTime = 0;
            player._lastMovement = Environment.TickCount;
            player._maxTimeCalled = false;

            //Throw ourselves onto our new team!
            team.addPlayer(player);

            //Destroy our spectator vehicle
            player._occupiedVehicle.destroy(true);
            player._bSpectator = false;

            //Set relative vehicle if required, no need for any if statement here :]
            VehInfo vehicle = player._server._assets.getVehicleByID(player.getDefaultVehicle().Id + player._server._zoneConfig.teams[team._id].relativeVehicle);
            player.setDefaultVehicle(vehicle);

            //Run the exit spec event
            Logic_Assets.RunEvent(player, player._server._zoneConfig.EventInfo.exitSpectatorMode);

            //Make sure the arena knows we've entered
            player._arena.playerEnter(player);

            if (player.ZoneStat1 > player._server._zoneConfig.bounty.start)
                player.Bounty = player.ZoneStat1;
            else
                player.Bounty = player._server._zoneConfig.bounty.start;

            return true;
        }
    }
}
