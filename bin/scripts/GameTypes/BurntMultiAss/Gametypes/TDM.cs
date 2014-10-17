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

        Dictionary<String, PlayerStats> _savedPlayerStats;

        public class PlayerStats
        {
            public int kills { get; set; }
            public int deaths { get; set; }
        }

        public TDM(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;

            config = arena._server._zoneConfig;
        }

        public void Initialize()
        {
            _savedPlayerStats = new Dictionary<string, PlayerStats>();
            settings.GameState = GameStates.Vote;
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
            UpdateTickers();
        }

        public void StartGame()
        {
            //Are we recording stats?
            arena._saveStats = true;

            //Start a new session for players, clears the old one
            _savedPlayerStats.Clear();

            foreach (Player p in arena.Players)
            {
                PlayerStats temp = new PlayerStats();
                temp.kills = 0;
                temp.deaths = 0;
                _savedPlayerStats.Add(p._alias, temp);
            }

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
            arena.sendArenaMessage(winningTeam._name + " has won the game!");
            winningTeam = null;
        }

        public void RestartGame()
        {
            StartGame();
            EndGame();
        }

        private void UpdateTickers()
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }
            string format;
            if (arena.ActiveTeams.Count() > 1)
            {
                //Team scores
                format = String.Format("{0}={1} - {2}={3}",
                    arena.ActiveTeams.ElementAt(0)._name,
                    arena.ActiveTeams.ElementAt(0)._currentGameKills,
                    arena.ActiveTeams.ElementAt(1)._name,
                    arena.ActiveTeams.ElementAt(1)._currentGameKills);
                arena.setTicker(1, 2, 0, format);

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
                    winningTeam = arena.ActiveTeams.ElementAt(1);
                    //?????
                }

                //Personal scores
                arena.setTicker(2, 1, 0, delegate(Player p)
                {
                    //Update their ticker
                    if (_savedPlayerStats.ContainsKey(p._alias))
                        return "Personal Score: Kills=" + _savedPlayerStats[p._alias].kills + " - Deaths=" + _savedPlayerStats[p._alias].deaths;

                    return "";
                });

                //1st and 2nd place with mvp (for flags later)
                IEnumerable<Player> ranking = arena.PlayersIngame.OrderByDescending(player => _savedPlayerStats[player._alias].kills);
                int idx = 3; format = "";
                foreach (Player rankers in ranking)
                {
                    if (!arena.Players.Contains(rankers))
                        continue;

                    if (idx-- == 0)
                        break;

                    switch (idx)
                    {
                        case 2:
                            format = String.Format("1st: {0}(K={1} D={2})", rankers._alias,
                              _savedPlayerStats[rankers._alias].kills, _savedPlayerStats[rankers._alias].deaths);
                            break;
                        case 1:
                            format = (format + String.Format(" 2nd: {0}(K={1} D={2})", rankers._alias,
                              _savedPlayerStats[rankers._alias].kills, _savedPlayerStats[rankers._alias].deaths));
                            break;
                    }
                }
                if (!arena.recycling)
                    arena.setTicker(2, 0, 0, format);
            }
        }

        public void PlayerKill(Player killer, Player victim)
        {
            if (settings.GameState == GameStates.ActiveGame)
            {
                if (_savedPlayerStats.ContainsKey(killer._alias))
                    _savedPlayerStats[killer._alias].kills++;
                if (_savedPlayerStats.ContainsKey(victim._alias))
                    _savedPlayerStats[victim._alias].deaths++;
            }
        }

        public void PlayerEnter(Player player)
        {
            //Nothing to do
        }

        public void PlayerEnterArena(Player player)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }
            if (!_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStats temp = new PlayerStats();
                temp.deaths = 0;
                temp.kills = 0;
                _savedPlayerStats.Add(player._alias, temp);
            }
        }

        public void PlayerLeaveGame(Player player)
        {
            //Nothing to do
        }
    }
}
