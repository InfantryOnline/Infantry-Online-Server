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
    public class CTF
    {
        private Arena arena;
        private Settings settings;
        private CfgInfo CFG;

        private int minPlayers;

        private Team winningTeam;
        private int winningTeamTick;
        private int victoryHoldTime;

        public CTF(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;

            minPlayers = Int32.MaxValue;

            foreach (Arena.FlagState fs in arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < minPlayers)
                    minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                //      fs.TeamChange += onFlagChange;
            }

            if (minPlayers == Int32.MaxValue)
            {

            }
        }

        private void Initialize()
        {
            victoryHoldTime = CFG.flag.victoryHoldTime;
        }

        public void Poll(int now)
        {      
            switch (settings.GameState)
            {
                case GameStates.ActiveGame:
                    switch (settings.CTFMode)
                    {
                        case CTFMode.ActiveGame:
                            PollCTF(now);
                            break;
                        case CTFMode.Init:
                            InitCTF();
                            settings.CTFMode = CTFMode.ActiveGame;
                            break;
                        case CTFMode.TenSeconds:
                            //10 second win timer
                            break;
                        case CTFMode.ThirtySeconds:
                            //30 second win timer
                            break;
                        case CTFMode.SixtySeconds:
                            //60 second win timer
                            break;
                        case CTFMode.XSeconds:
                            //Initial win timer upon capturing
                            break;
                        case CTFMode.GameDone:
                            //Game is done
                            break;
                        case CTFMode.NotEnoughPlayers:
                            //Not enough players in game, update ticker
                            break;
                    }
                    break;
                case GameStates.Init:
                    Initialize();
                    break;
                case GameStates.PreGame:
                    //Handled in main script
                    break;
                case GameStates.PostGame:
                    settings.GameState = GameStates.Init;
                    break;
                case GameStates.Vote:
                    //Handled in main script
                    break;                
            }
        }

        private void PollCTF(int now)
        {
            //See if we have enough players to keep playing
            if (arena.PlayersIngame.Count() < CFG.flag.startDelay)
            {
                settings.CTFMode = CTFMode.NotEnoughPlayers;
            }
            else
            {
                //Signal the game is starting
                settings.CTFMode = CTFMode.Init;
            }

            //See if someone is winning
            if (winningTeam != null)
            {
                int tick = now - winningTeamTick;
                switch (tick)
                {
                    case 10:
                        break;
                    case 30:
                        break;
                    case 60:
                        break;
                    case 0:
                        //They just grabbed it
                        break;

                }
            }
        }

        private void InitCTF()
        {

        }

        public void StartGame()
        {
            //Reset flags            
        }

        public void EndGame()
        {
            //Reset winning team
        }

        public void RestartGame()
        {
            StartGame();
            EndGame();
        }

        public void PlayerKill(Player killer, Player recipient)
        {
        }

        public void PlayerEnter()
        {

        }
        public void PlayerEnterArena()
        {

        }
        public void PlayerLeaveGame()
        {

        }
    }
}
