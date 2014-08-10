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
    public class KOTH
    {
        //Settings
        private Arena arena;
        private Settings settings;
        private CfgInfo config;

        private Dictionary<Player, PlayerCrownStatus> _playerCrownStatus;

        private class PlayerCrownStatus
        {
            public bool crown;                  //Player has crown?
            public int crownKills;              //Crown kills without a crown
            public int crownDeaths;             //Times died with a crown (counted until they lose it)
            public int expireTime;              //When the crown will expire
            public PlayerCrownStatus(bool bCrown)
            {
                crown = bCrown;
            }
            public PlayerCrownStatus()
            {
                crown = true;
            }
        }

        public KOTH(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;

            config = arena._server._zoneConfig;
        }

        private void Initialize()
        {
            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
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
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            foreach (var p in _playerCrownStatus)
            {//Find our expired players
                if ((now > p.Value.expireTime) && p.Value.crown)
                {
                    p.Value.crown = false;
                    //Tell everyone about it
                    Helpers.Player_Crowns(arena, true, GetCrowns());
                    Helpers.Player_Crowns(arena, false, GetNoCrowns());
                }
            }

            updateKOTHTickers();

            if (GetWinner() != null || GetCrowns().Count == 0)
            {//End the game if there is a chicken dinner
                arena.gameEnd();
            }

        }

        public void StartGame()
        {   //Game is starting
            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            _playerCrownStatus.Clear();

            //Crown everyone
            foreach (Player player in arena.PlayersIngame)
            {
                _playerCrownStatus.Add(player, new PlayerCrownStatus());
                AddCrown(player);
            }

            //Everybody has corn
            Helpers.Player_Crowns(arena, true, arena.PlayersIngame.ToList());

            arena.sendArenaMessage("King of the Hill has started!", 1);
        }

        public void EndGame()
        {	//Game finished
            Team winner = GetWinner();

            if (winner != null)
            {
                arena.sendArenaMessage(winner._name + " has won the game!");

                //Calculate the jackpot for each player
                foreach (Player p in winner.AllPlayers)
                {	//Spectating? 
                    if (p.IsSpectator)
                        continue;

                    //Obtain the respective rewards
                    int cashReward = config.king.cashReward;
                    int experienceReward = config.king.experienceReward;
                    int pointReward = config.king.pointReward;

                    p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                    //Prize winning team
                    p.Cash += cashReward;
                    p.Experience += experienceReward;
                    p.BonusPoints += pointReward;
                }
            }
            else
            {
                arena.sendArenaMessage("No one won the game");
            }

            //Remove all crowns
            _playerCrownStatus.Clear();
            Helpers.Player_Crowns(arena, false, arena.Players.ToList());

            //End the game officially
            settings.GameState = GameStates.PostGame;
        }

        public void ResetGame()
        {
            StartGame();
            EndGame();
        }

        public void updateKOTHTickers()
        {
            if (arena.ActiveTeams.Count() > 1)
            {//Show players their crown timer using a ticker
                arena.setTicker(1, 0, 0, delegate(Player p)
                {
                    if (_playerCrownStatus.ContainsKey(p) && _playerCrownStatus[p].crown)
                    {
                        return String.Format("Corn Timer: {0}", (_playerCrownStatus[p].expireTime - Environment.TickCount) / 1000);
                    }
                    else
                    {
                        return "You have no corn";
                    }
                });
            }
        }

        private void AddCrown(Player player)
        {
            PlayerCrownStatus status = _playerCrownStatus[player];

            status.crown = true;
            status.crownDeaths = 0;
            status.crownKills = 0;

            Helpers.Player_Crowns(arena, true, GetCrowns());
            UpdateCrown(player);
        }

        private List<Player> GetCrowns()
        {
            List<Player> output = new List<Player>();
            
            foreach (Player player in arena.PlayersIngame)
            {
                if (!_playerCrownStatus.ContainsKey(player))
                {
                    continue;
                }
                if (_playerCrownStatus[player].crown)
                {
                    output.Add(player);
                }
            }

            return output;
        }

        private List<Player> GetNoCrowns()
        {
            List<Player> output = new List<Player>();

            foreach (Player player in arena.PlayersIngame)
            {
                if (!_playerCrownStatus.ContainsKey(player))
                {
                    continue;
                }

                if (!_playerCrownStatus[player].crown)
                    output.Add(player);
            }

            return output;
        }

        private Team GetWinner()
        {
            List<Player> Crowners = GetCrowns();

            if (Crowners.Count >= 1)
            {
                //There is at least one person with a crown left
                List<Team> PossibleTeams = new List<Team>();

                foreach (Player player in Crowners)
                {
                    if (!PossibleTeams.Contains(player._team))
                    {
                        PossibleTeams.Add(player._team);
                    }
                }

                if (PossibleTeams.Count > 1)
                {//There are no winners yet, 2 or more teams have a crowner
                    return null;
                }
                else
                {//We have a winning team
                    return PossibleTeams.First();
                }
            }
            else
            {//It was a tie -- no crowners when we checked
                return null;
            }
        }

        private void UpdateCrown(Player player)
        {
            _playerCrownStatus[player].expireTime = Environment.TickCount + (config.king.expireTime * 1000);
        }

        public void PlayerKill(Player killer, Player victim)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            if (_playerCrownStatus[victim].crown)
            {   //Incr crownDeaths
                _playerCrownStatus[victim].crownDeaths++;

                if (_playerCrownStatus[victim].crownDeaths >= config.king.deathCount)
                {
                    //Take it away now
                    _playerCrownStatus[victim].crown = false;
                    Helpers.Player_Crowns(arena, false, GetNoCrowns());
                }

                if (!_playerCrownStatus[killer].crown)
                {
                    _playerCrownStatus[killer].crownKills++;
                }
            }

            //Reset their timer
            if (_playerCrownStatus[killer].crown)
            {
                UpdateCrown(killer);
            }
            else if (config.king.crownRecoverKills != 0)
            {   //Should they get a crown?
                if (_playerCrownStatus[killer].crownKills >= config.king.crownRecoverKills)
                {
                    _playerCrownStatus[killer].crown = true;
                    AddCrown(killer);
                }
            }
        }

        public void PlayerEnter(Player player)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            //Send them the crowns and add them back to list incase a game is in progress
            if (!_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player] = new PlayerCrownStatus(false);
                Helpers.Player_Crowns(arena, true, GetCrowns(), player);
            }
        }

        public void PlayerEnterArena(Player player)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            //Send them the crowns..
            if (!_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player] = new PlayerCrownStatus(false);
                Helpers.Player_Crowns(arena, true, GetCrowns(), player);
            }
        }
        public void PlayerLeaveGame(Player player)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            if (_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player].crown = false;
                Helpers.Player_Crowns(arena, false, GetNoCrowns());
            }
        }

    }
}
