using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Script.GameType_Burnt
{
    public class Gladiator
    {
        private Arena arena;                //The arena we are in 
        private Settings settings;       //State of the game

        private int spawnX1;                //Spawn point for team 1 
        private int spawnY1;

        private int spawnX2;                //Spawn point for team 2
        private int spawnY2;
        
        private Dictionary<Team, List<Player>> participants;

        public Gladiator(Arena arena, Settings settings)
        {
            this.arena = arena;
            this.settings = settings;

            spawnX1 = 1;                    //Get coords from burnt
            spawnY1 = 2;
            spawnX2 = 3;
            spawnY2 = 4;            
        }

        public void Initialize()
        {
            participants = new Dictionary<Team, List<Player>>();
            PreGame();
        }

        public void Poll(int now)
        {
            switch (settings.GameState)
            {
                case GameStates.Init:
                    Initialize();
                    break;
                case GameStates.Vote:
                    break;
                case GameStates.PreGame:                 
                    break;
                case GameStates.ActiveGame:
                    Poll();
                    break;
                case GameStates.PostGame:
                    settings.GameState = GameStates.Init;
                    break;
            }
        }

        private void Poll()
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            if (findWinner() != null)
            {
                arena.gameEnd();
            }
        }

        private void PreGame()
        {
            settings.GameState = GameStates.PreGame;
            arena.setTicker(1, 1, Settings.VotingPeriod * 100, "Gladiator Event: ",
                    delegate()
                    {
                        settings.GameState = GameStates.ActiveGame;
                        arena.gameStart();
                    }
            );
        }
                
        public void StartGame()
        {
            arena.sendArenaMessage("[Event] Gladiator event is now starting");

            //Spawn everyone that is not in spec in the event room
            foreach (Player player in arena.PlayersIngame.ToList())
            {
                if (participants[player._team].Contains(player))
                {//They are in the list already somehow
                    continue;
                }
                if (player._team == arena.ActiveTeams.ElementAt(0))
                {//Team 1 
                    player.warp(spawnX1, spawnY1);
                    participants[player._team].Add(player);
                }
                else if (player._team == arena.ActiveTeams.ElementAt(1))
                {//Team 2
                    player.warp(spawnX2, spawnY2);
                    participants[player._team].Add(player);
                }
                else
                {
                    //Unhandled -- there are only two teams 
                    continue;
                }
            }
        }

        public void EndGame()
        {
            arena.sendArenaMessage("[Event] Gladiator event is now over");

            //Find the winner
            Team winner = findWinner();

            if (winner == null)
            {
                arena.sendArenaMessage("[Event] There was no winner");
                return;
            }

            arena.sendArenaMessage("[Event] The winner is Team " + winner._name);     

            //Reward them            
        }

        /// <summary>
        /// Resets the game
        /// </summary>
        public void ResetGame()
        {
            EndGame();
            StartGame();
        }

        public void PlayerKill(Player killer, Player recipient)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            if (participants[recipient._team].Contains(recipient))
            {//Remove them entirely
                participants[recipient._team].Remove(recipient);
            }
        }

        public void PlayerEnter(Player player)
        {
            //Nothing to do here
        }
        public void PlayerEnterArena(Player player)
        {
            //Nothing to do here
        }

        public void PlayerLeaveGame(Player player)
        {
            if (settings.GameState != GameStates.ActiveGame)
            {
                return;
            }

            //Check if they were part of the event
            //If so remove them from the list of people participating in the event
            if (participants[player._team].Contains(player))
            {
                participants[player._team].Remove(player);
            }
        }
                
        private Team findWinner()
        {
            foreach (KeyValuePair<Team, List<Player>> pair in participants.ToList())
            {
                if (pair.Value.Count != 0)
                {
                    continue;
                }
                return pair.Key;
            }

            return null;
        }
    }
}
