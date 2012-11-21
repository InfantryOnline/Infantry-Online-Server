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

namespace InfServer.Script.GameType_Dodgeball
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_Dodgeball : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config

		private Team _victoryTeam;				//The team currently winning!
        private Dictionary<Team, int> _teams;   //Our teams, and how many players left.
        private Random _rand;		
        Team team1;
        Team team2;
        int team1Count;
        int team2Count;
        Dictionary<Player, int> queue;
        Dictionary<Player, bool> inPlayers;
		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;				//The tick at which the game started (0 == stopped)
		//Settings
		private int _minPlayers;				//The minimum amount of players
        private bool bVictory = false;



		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Performs script initialization
		/// </summary>
		public bool init(IEventObject invoker)
		{	//Populate our variables
			_arena = invoker as Arena;
			_config = _arena._server._zoneConfig;
            _rand = new Random();

            team1 = _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.getTeamByName(_config.teams[1].name);
            _minPlayers = 2;
            queue = new Dictionary<Player, int>();
			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Do we have enough players ingame?
            int playing = _arena.PlayerCount;

            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                _arena.gameReset();
            }

            if (_tickGameStart != 0)
            {                
                if (team1Count == 0 || team2Count == 0)
                {   //Victory
                    gameEnd();
                }
            }

           //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, 8 * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start

                        _arena.gameStart();
                    }
                );
            }
            return true;
        }



		#region Events
	
        /// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {	//Is a hit?
            if (weapon.id == 1011)
            {   //Are they trying to catch?
                IEnumerable<Player> players = player._arena.getPlayersInRange(posX, posY, 15);
                if (players.Count() > 0)
                {
                    IEnumerable<Player> teamPlayers = players.First()._team.ActivePlayers.OrderBy(plyr => _rand.Next(0, 200));
                    if (players.First().getInventoryAmount(28) > 0)
                    {//Yes, lets summon a teammate back in
                        foreach (Player p in teamPlayers)
                        {
                            if (p.IsDead)
                                continue;

                            if (inPlayers.ContainsKey(p))
                                continue;

                            if (p._team == team1)
                                team1Count++;
                            else
                                team2Count++;

                            _arena.triggerMessage(5, 500, String.Format("{0} caught {1}'s ball and returned {2} to the court.", players.First()._alias, player._alias, p._alias));
                            players.First().Cash += 800;
                            players.First().sendMessage(0, "Catch award: (Cash=800)");
                            inPlayers.Add(p, true);
                            Player warpTo = players.First();
                            p.warp(warpTo._state.positionX + _rand.Next(0, 15), warpTo._state.positionY + _rand.Next(0, 15));
                            break;
                        }
                    }
                }


            }
            return true;
        }

		/// <summary>
		/// Called when a player enters the game
		/// </summary>
		[Scripts.Event("Player.Enter")]
		public void playerEnter(Player player)
		{
		}
		
		/// <summary>
		/// Called when a player enters the arena
		/// </summary>
		[Scripts.Event("Player.EnterArena")]
		public void playerEnterArena(Player player)
		{
		}

		/// <summary>
		/// Called when a player leaves the game
		/// </summary>
		[Scripts.Event("Player.Leave")]
		public void playerLeave(Player player)
		{
            updateQueue(queue);
		}


		/// <summary>
		/// Called when the game begins
		/// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            inPlayers = new Dictionary<Player, bool>();

            foreach (Player p in _arena.PlayersIngame)
            {
                inPlayers.Add(p, true);
                int x = 0;
                p.setVar("Hits", x);
            }
            team1Count = team1.ActivePlayerCount;
            team2Count = team2.ActivePlayerCount;

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

            return true;
        }

		/// <summary>
		/// Called when the game ends
		/// </summary>
		[Scripts.Event("Game.End")]
		public bool gameEnd()
		{	//Game finished, perhaps start a new one


			_tickGameStart = 0;
			_tickGameStarting = 0;
			_victoryTeam = null;



            int pcount = 0;

            if (team1Count > 0)
            {
                _victoryTeam = team1;
                pcount = team1Count;
            }
            else
            {
                _victoryTeam = team2;
                pcount = team2Count;
            }

            _arena.sendArenaMessage("Game Over");
            _arena.sendArenaMessage(String.Format("&{0} are victorious with {1} player(s) left", _victoryTeam._name, pcount));


            IEnumerable<Player> rankedPlayers;
            int idx;
            rankedPlayers = _arena.PlayersIngame.OrderByDescending(
                    p => p.getVarInt("Hits"));
            idx = 3;	//Only display top three players

            foreach (Player p in rankedPlayers)
            {

                if (idx-- == 0)
                    break;

                //Set up the format
                string format = "!3rd - (Hits={0}): {1}";

                switch (idx)
                {
                    case 2:
                        format = "!1st - (Hits={0}): {1}";
                        break;
                    case 1:
                        format = "!2nd - (Hits={0}): {1}";
                        break;
                }

                _arena.sendArenaMessage(String.Format(format, p.getVarInt("Hits"), p._alias));
            }

            foreach (Player p in _arena.Players)
            {
                int hits = p.getVarInt("Hits");
                int cash = 300 * hits;
                int experience = 200 * hits;
                int points = 100 * hits;
                p.Cash += cash;
                p.KillPoints += points;
                p.ExperienceTotal += experience;
                p.sendMessage(0, String.Format("Personal Award: (Cash={0}) (Experience={1}) (Points={2})", cash, experience, points)); 
                p.resetVars();
                p.syncState();
            }

            //Shuffle the players up randomly into a new list
            var random = _rand;
            Player[] shuffledPlayers = _arena.PlayersIngame.ToArray(); //Arrays ftw
            for (int i = shuffledPlayers.Length - 1; i >= 0; i--)
            {
                int swap = random.Next(i + 1);
                Player tmp = shuffledPlayers[i];
                shuffledPlayers[i] = shuffledPlayers[swap];
                shuffledPlayers[swap] = tmp;
            }

            //Assign the new list of players to teams
            int j = 1;
            foreach (Player p in shuffledPlayers)
            {
                if (j <= Math.Ceiling((double)shuffledPlayers.Length / 2)) //Team 1 always get the extra player :)
                {
                    if (p._team != team1) //Only change his team if he's not already on the team d00d
                        team1.addPlayer(p);
                }
                else
                {
                    if(p._team != team2)
                        team2.addPlayer(p);
                }
                j++;

            }

            //Notify players of the scramble
            _arena.sendArenaMessage("Teams have been scrambled!");

			return false;
		}

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Game.Breakdown")]
        public bool breakdown()
        {	//Allows additional "custom" breakdown information

            //Always return true;
            return true;
        }

		/// <summary>
		/// Called to reset the game state
		/// </summary>
		[Scripts.Event("Game.Reset")]
		public bool gameReset()
		{	//Game reset, perhaps start a new one
			_tickGameStart = 0;
			_tickGameStarting = 0;

			_victoryTeam = null;

			return true;
		}

		/// <summary>
		/// Handles the spawn of a player
		/// </summary>
		[Scripts.Event("Player.Spawn")]
		public bool playerSpawn(Player player, bool bDeath)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to unspec and join the game
		/// </summary>
		[Scripts.Event("Player.JoinGame")]
		public bool playerJoinGame(Player player)
		{
            if (_arena.PlayersIngame.Count() == _config.arena.playingMax)
                enqueue(player);
			return true;
		}

        /// <summary>
        /// Enqueues a player to unspec when there is an opening.
        /// </summary>
        /// <param name="player"></param>
        public void enqueue(Player player)
        {
            if (!queue.ContainsKey(player))
            {
                queue.Add(player, queue.Count());
                player.sendMessage(-1, String.Format("The game is full, (Queue={0})", queue[player]));
            }
            else
            {
                queue.Remove(player);
                player.sendMessage(-1, "Removed from queue");
            }
        }

        public void updateQueue(Dictionary<Player, int> queue)
        {   //Nonsense!
            if (_arena.PlayersIngame.Count() == _config.arena.playingMax)
                return;

            if (queue.Count > 0)
            {

                if (team1.ActivePlayerCount < 8)
                    queue.ElementAt(0).Key.unspec(team1._name);
                else if (team2.ActivePlayerCount < 8)
                    queue.ElementAt(0).Key.unspec(team2._name);

                queue.Remove(queue.ElementAt(0).Key);

                foreach (KeyValuePair<Player, int> player in queue)
                {
                    queue[player.Key] = queue[player.Key] - 1;
                    player.Key.sendMessage(0, String.Format("Queue position is now {0}", queue[player.Key]));
                }
            }
        }

		/// <summary>
		/// Triggered when a player wants to spec and leave the game
		/// </summary>
		[Scripts.Event("Player.LeaveGame")]
		public bool playerLeaveGame(Player player)
		{
            if (inPlayers.ContainsKey(player))
            {
                if (player._team == team1)
                    team1Count--;
                else
                    team2Count--;

                inPlayers.Remove(player);
                return true;
            }

            updateQueue(queue);
			return true;
		}

		/// <summary>
		/// Triggered when a player has died, by any means
		/// </summary>
		/// <remarks>killer may be null if it wasn't a player kill</remarks>
		[Scripts.Event("Player.Death")]
		public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
		{
			return true;
		}

		/// <summary>
		/// Triggered when one player has killed another
		/// </summary>
		[Scripts.Event("Player.PlayerKill")]
		public bool playerPlayerKill(Player victim, Player killer)
		{
            if (victim._team == team1)
                team1Count--;
            else
                team2Count--;


            killer.setVar("Hits", killer.getVarInt("Hits") + 1);
            inPlayers.Remove(victim);
			return true;
		}
		#endregion
	}
}