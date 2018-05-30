using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Burnt
{
    public class TDM
    {
        private Arena arena;
        private Settings settings;
        private CfgInfo config;
        private Team winningTeam;
        private int minPlayers;

        public TDM(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;

            config = arena._server._zoneConfig;
            minPlayers = config.deathMatch.minimumPlayers;
        }

        private void Initialize()
        {

        }

        public void Poll(int now)
        {
            switch (settings.GameState)
            {
                case GameStates.Init:
                    Initialize();
                    break;
                case GameStates.Vote:
                    //Handled in main script
                    break;
                case GameStates.PreGame:
                    //Handled in main script
                    break;
                case GameStates.ActiveGame:
                    PollGame(now);
                    break;
                case GameStates.PostGame:
                    settings.GameState = GameStates.Init;
                    break;
            }
        }

        private void PollGame(int now)
        {
            if (arena.PlayersIngame.Count() < minPlayers)
            {   //Notify
                arena.setTicker(1, 3, 0, "Not Enough Players");
                arena.gameEnd();
            }
            UpdateTickers();
        }

        public void StartGame()
        {
            //Let everyone know
            arena.sendArenaMessage("Game has started!", 1);
            arena.setTicker(1, 3, config.deathMatch.timer * 100, "Time Left: ",
                delegate()
                {	//Trigger game end.
                    arena.gameEnd();
                }
            );
        }

        public void EndGame()
        {
            if (winningTeam == null)
            {
                arena.sendArenaMessage("There was no winner.");
            }
            else
            {
                arena.sendArenaMessage(winningTeam._name + " has won the game!");
                winningTeam = null;
            }
        }

        public void RestartGame()
        {
            EndGame();
            StartGame();
        }

        private void UpdateTickers()
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }
            string formatTeam;
            if (arena.ActiveTeams.Count() > 1)
            {
                //Team scores
                formatTeam = string.Format("{0}={1} - {2}={3}",
                    arena.ActiveTeams.ElementAt(0)._name,
                    arena.ActiveTeams.ElementAt(0)._currentGameKills,
                    arena.ActiveTeams.ElementAt(1)._name,
                    arena.ActiveTeams.ElementAt(1)._currentGameKills);
                arena.setTicker(1, 2, 0, formatTeam);

                //Mark winner for posterity
                if (arena.ActiveTeams.ElementAt(0)._currentGameKills > arena.ActiveTeams.ElementAt(1)._currentGameKills)
                {
                    winningTeam = arena.ActiveTeams.ElementAt(0);
                }
                else if (arena.ActiveTeams.ElementAt(0)._currentGameKills < arena.ActiveTeams.ElementAt(1)._currentGameKills)
                {
                    winningTeam = arena.ActiveTeams.ElementAt(1);
                }
                else
                {//Tie
                    winningTeam = null;
                    //?????
                }
            }
            //Personal scores
            arena.setTicker(2, 1, 0, delegate(Player p)
            {
                //Update their ticker
                if (p.StatsCurrentGame == null)
                {
                    return "Personal Score: Kills=0 - Deaths=0";
                }

                return "Personal Score: Kills=" + p.StatsCurrentGame.kills + " - Deaths=" + p.StatsCurrentGame.deaths;
            });

            //1st and 2nd place
            int idx = 3; string format = "";
            var ranked = arena.Players.Select(player => new
            {
                Alias = player._alias,
                Kills = (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.kills),
                Deaths = (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.deaths)
            })
            .GroupBy(pl => pl.Kills)
            .OrderByDescending(k => k.Key)
            .Take(idx)
            .Select(g => g.OrderBy(plyr => plyr.Deaths));

            foreach (var group in ranked)
            {
                if (idx <= 0)
                    break;

                string placement = "";
                format = " (K={0} D={1}): {2}";
                switch (idx)
                {
                    case 3:
                        placement = "!1st";
                        break;
                    case 2:
                        placement = "!2nd";
                        break;
                    case 1:
                        placement = "!3rd";
                        break;
                }
                idx -= group.Count();
                if (group.First() != null)
                {
                    format = string.Format(placement + format, group.First().Kills, group.First().Deaths, string.Join(", ", group.Select(g => g.Alias)));
                }
            }
            if (!arena.recycling)
                arena.setTicker(2, 0, 0, format);
        }

        public void PlayerKill(Player killer, Player victim)
        {
            //Nothing to do
        }

        public void PlayerEnter(Player player)
        {
            //Nothing to do
        }

        public void PlayerEnterArena(Player player)
        {
            //Nothing to do
        }

        public void PlayerLeaveGame(Player player)
        {
            //Nothing to do
        }

        public void PlayerLeaveArena(Player player)
        {
            //Nothing to do
        }

    }
}