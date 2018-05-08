using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Game;
using Assets;

namespace InfServer.Script.GameType_Burnt
{
    public class CTF
    {
        private Arena arena;
        private Settings settings;
        private CfgInfo CFG;

        private int minPlayers;

        private Team winningTeam;
        private bool gameWon;
        private int winningTeamTick;
        private int victoryHoldTime;

        public CTF(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;

            minPlayers = int.MaxValue;

            foreach (Arena.FlagState fs in arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < minPlayers)
                    minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += OnFlagChange;
            }

            if (minPlayers == int.MaxValue)
            {
                minPlayers = 1;
            }
        }

        private void Initialize()
        {
            victoryHoldTime = CFG.flag.victoryHoldTime;
            settings.GameState = GameStates.Vote;
        }

        private void OnFlagChange(Arena.FlagState flag)
        {
            Team victory = flag.team;

            //Does this team now have all the flags?
            foreach(Arena.FlagState fs in arena._flags.Values)
            {
                if (fs.bActive && fs.team != flag.team)
                {   //Not all flags are captured yet
                    victory = null;
                }
            }

            if (!gameWon)
            {
                if (victory != null)
                {
                    winningTeam = victory;
                    winningTeamTick = Environment.TickCount;
                    settings.CTFMode = CTFMode.XSeconds;
                }
                else
                {
                    if (winningTeam != null)
                    {
                        winningTeam = null;
                        winningTeamTick = 0;
                        settings.CTFMode = CTFMode.Aborted;
                    }
                }
            }
        }

        private void CheckWinner(int now)
        {
            //See if someone is winning
            if (winningTeam != null)
            {
                int tick = now - winningTeamTick;
                switch (tick)
                {
                    case 10:
                        settings.CTFMode = CTFMode.TenSeconds;
                        break;
                    case 30:
                        settings.CTFMode = CTFMode.ThirtySeconds;
                        break;
                    case 60:
                        settings.CTFMode = CTFMode.SixtySeconds;
                        break;
                    default:
                        if (tick <= 0)
                        {
                            settings.CTFMode = CTFMode.GameDone;
                        }
                        break;
                }
            }
        }

        public void Poll(int now)
        {      
            switch (settings.GameState)
            {
                case GameStates.ActiveGame:
                    PollCTF(now);
                    break;
                case GameStates.Init:
                    Initialize();
                    InitCTF();
                    break;
                case GameStates.Vote:
                    //Handled in main script
                    break;
                case GameStates.PreGame:
                    //Handled in main script
                    break;
                case GameStates.PostGame:
                    settings.GameState = GameStates.Init;
                    break;
            }
        }

        private void PollCTF(int now)
        {
            //See if we have enough players to keep playing
            if (arena.PlayersIngame.Count() < minPlayers)
            {
                settings.CTFMode = CTFMode.NotEnoughPlayers;
            }
            else
            {
                CheckWinner(now);
            }

            int countdown = winningTeamTick > 0 ? (CFG.flag.victoryHoldTime / 10) - ((now - winningTeamTick) / 1000) : 0;
            switch (settings.CTFMode)
            {
                case CTFMode.Aborted:
                    arena.sendArenaMessage("Victory has been aborted.", CFG.flag.victoryAbortedBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.TenSeconds:
                    //10 second win timer
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.ThirtySeconds:
                    //30 second win timer
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.SixtySeconds:
                    //60 second win timer
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.XSeconds:
                    //Initial win timer upon capturing
                    arena.setTicker(4, 1, CFG.flag.victoryHoldTime, "Victory in ");
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.GameDone:
                    //Game is done
                    gameWon = true;
                    arena.gameEnd();
                    break;
                case CTFMode.NotEnoughPlayers:
                    //Not enough players in game, update ticker
                    arena.setTicker(1, 1, 0, "Not Enough Players");
                    break;
            }

            updateCTFTickers();
        }

        private void InitCTF()
        {
            winningTeamTick = 0;
            winningTeam = null;
            gameWon = false;
        }

        public void StartGame()
        {
            settings.CTFMode = CTFMode.ActiveGame;

            //Let everyone know
            arena.sendArenaMessage("Game has started!", CFG.flag.resetBong);
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

        private void updateCTFTickers()
        {
            List<Player> rankedPlayers = arena.Players.ToList().OrderBy(player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.deaths)).OrderByDescending(
                player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.kills)).ToList();
            int idx = 3;
            string format = "";
            foreach(Player p in rankedPlayers)
            {
                if (p.StatsCurrentGame == null)
                { continue; }
                if (idx-- == 0)
                {
                    break;
                }

                switch(idx)
                {
                    case 2:
                        format = string.Format("!1st (K={0} D={1}): {2}", p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths, p._alias);
                        break;
                    case 1:
                        format = (format + string.Format("!2nd (K={0} D={1}): {2}", p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths, p._alias));
                        break;
                }
            }
            if (!string.IsNullOrWhiteSpace(format))
            { arena.setTicker(0, 2, 0, format); }

            arena.setTicker(2, 3, 0, delegate (Player p)
            {
                if (p.StatsCurrentGame == null)
                {
                    return "Personal Score: Kills=0 - Deaths=0";
                }
                return string.Format("Personal Score: Kills={0} - Deaths{1}", p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
            });
        }

        public void PlayerKill(Player killer, Player recipient)
        {
        }

        public void PlayerEnter(Player player)
        {

        }
        public void PlayerEnterArena(Player player)
        {

        }
        public void PlayerLeaveGame(Player player)
        {

        }

        public void PlayerLeaveArena(Player player)
        {

        }
    }
}
