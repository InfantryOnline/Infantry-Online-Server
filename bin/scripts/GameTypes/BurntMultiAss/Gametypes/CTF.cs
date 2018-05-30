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
        private int winningTeamNotify;
        private int victoryHoldTime;

        public CTF(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;
            this.CFG = arena._server._zoneConfig;

            minPlayers = int.MaxValue;

            foreach (Arena.FlagState fs in arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < minPlayers)
                { minPlayers = fs.flag.FlagData.MinPlayerCount; }

                //Register our flag change events
                fs.TeamChange += OnFlagChange;
            }

            if (minPlayers == int.MaxValue)
            {
                minPlayers = 2;
            }
        }

        private void Initialize()
        {
            victoryHoldTime = CFG.flag.victoryHoldTime;
        }

        private void OnFlagChange(Arena.FlagState flag)
        {
            Team victory = flag.team;

            //Does this team now have all the flags?
            foreach (Arena.FlagState fs in arena._flags.Values)
            {
                if (fs.bActive && fs.team != flag.team)
                {   //Not all flags are captured yet
                    victory = null;
                    break;
                }
            }

            if (!gameWon)
            {
                if (victory != null)
                {
                    winningTeamTick = (Environment.TickCount + (victoryHoldTime * 10));
                    winningTeamNotify = 0;
                    winningTeam = victory;
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
                //Has XSeconds been called yet?
                if (settings.CTFMode == CTFMode.XSeconds)
                { return; }

                int tick = ((winningTeamTick - now) / 1000);
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

        private void SetNotifyBypass(int countdown)
        {   //If XSeconds matches one of these, it will bypass that call
            //so there is no duplicate Victory message
            switch (countdown)
            {
                case 10:
                    winningTeamNotify = 1;
                    break;
                case 30:
                    winningTeamNotify = 2;
                    break;
                case 60:
                    winningTeamNotify = 3;
                    break;
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

            int countdown = winningTeamTick > 0 ? ((winningTeamTick - now) / 1000) : 0;
            switch (settings.CTFMode)
            {
                case CTFMode.Aborted:
                    arena.setTicker(4, 1, 0, "");
                    arena.sendArenaMessage("Victory has been aborted.", CFG.flag.victoryAbortedBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.TenSeconds:
                    //10 second win timer
                    if (winningTeamNotify == 1) //Been notified already?
                    { break; }
                    winningTeamNotify = 1;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.ThirtySeconds:
                    //30 second win timer
                    if (winningTeamNotify == 2) //Been notified already?
                    { break; }
                    winningTeamNotify = 2;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.SixtySeconds:
                    //60 second win timer
                    if (winningTeamNotify == 3) //Been notified already?
                    { break; }
                    winningTeamNotify = 3;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    settings.CTFMode = CTFMode.ActiveGame;
                    break;
                case CTFMode.XSeconds:
                    //Initial win timer upon capturing
                    SetNotifyBypass(countdown); //Checks to see if xSeconds matches any other timers
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
                    arena.setTicker(2, 1, 0, "Not Enough Players");
                    settings.GameState = GameStates.Init;
                    break;
            }

            updateCTFTickers();
        }

        private void InitCTF()
        {
            winningTeamNotify = 0;
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
            foreach (Player p in rankedPlayers)
            {
                if (p.StatsCurrentGame == null)
                { continue; }
                if (idx-- == 0)
                {
                    break;
                }

                switch (idx)
                {
                    case 2:
                        format = string.Format("!1st: {0}(K={1} D={2}) ", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
                        break;
                    case 1:
                        format = (format + string.Format("!2nd: {0}(K={1} D={2})", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths));
                        break;
                }
            }
            if (!string.IsNullOrWhiteSpace(format))
            { arena.setTicker(1, 2, 0, format); }

            arena.setTicker(2, 3, 0, delegate(Player p)
            {
                if (p.StatsCurrentGame == null)
                {
                    return "Personal Score: Kills=0 - Deaths=0";
                }
                return string.Format("Personal Score: Kills={0} - Deaths={1}", p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
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