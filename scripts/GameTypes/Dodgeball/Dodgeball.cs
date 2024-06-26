﻿using System;
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

        private Random _rand;		
        Team team1;
        Team team2;
        int team1Count;
        int team2Count;
        Dictionary<Player, int> queue;
        Dictionary<Player, bool> inPlayers;
		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		//Settings
		private int _minPlayers;				//The minimum amount of players



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

            _minPlayers = 2;

            team1 = _arena.getTeamByName(_config.teams[0].name);
            team2 = _arena.getTeamByName(_config.teams[1].name);
            queue = new Dictionary<Player, int>();
            inPlayers = new Dictionary<Player, bool>();
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

            //If game is running or starting and we don't have enough players
            if ((_arena._bGameRunning || _tickGameStarting != 0) && playing < _minPlayers)
            {   //Stop the game!
                _arena.gameEnd();
            }

            //If game is not running or starting, show the not enough players
            if ((!_arena._bGameRunning || _tickGameStarting == 0) && playing < _minPlayers)
            {
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                _arena.gameReset();
            }

            if (_arena._bGameRunning)
            {                
                if (team1Count == 0 || team2Count == 0)
                {   //Victory
                    _arena.gameEnd();
                }
            }

           //Do we have enough players to start a game?
            if (!_arena._bGameRunning && _tickGameStarting == 0 && playing >= _minPlayers)
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
        {
            //Log.write(TLog.Warning, "Explosion: X={0}, Y={1}, Z={2}, Wep={3}, Player={4}", posX, posY, posZ, weapon.id, player);

            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.DamageEvent")]
        public bool playerDamageEvent(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            //Are they trying to catch?
            if (player.getInventoryAmount(28) > 0 && inPlayers.ContainsKey(player))
            {
                //Yes, lets summon a teammate back in
                IEnumerable<Player> teamPlayers = player._team.ActivePlayers.OrderBy(plyr => _rand.Next(0, 200));
                foreach (Player p in teamPlayers)
                {
                    if (p.IsDead)
                        continue;

                    if (p == player)
                        continue;

                    if (inPlayers.ContainsKey(p))
                        continue;

                    inPlayers.Add(p, true);
                    if (p._team == team1)
                        team1Count++;
                    else
                        team2Count++;

                    _arena.triggerMessage(5, 500, String.Format("{0} caught a ball and returned {1} to the court.", player._alias, p._alias));
                    player.setVar("Catches", player.getVarInt("Catches") + 1);
                    player.Cash += 800;
                    player.sendMessage(0, "Catch award: (Cash=800)");
                    p.warp(player._state.positionX + _rand.Next(0, 15), player._state.positionY + _rand.Next(0, 15));
                    break;
                }
                //Log.write(TLog.Warning, "Player catch {0}", player);
                //Run the damage event
                return false;
            }
            //Stop the damage event from being run
            return true;
        }

		/// <summary>
		/// Called when a player unspecs
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
            if (inPlayers.ContainsKey(player))
            {
                if (player._team == team1)
                    team1Count--;
                else
                    team2Count--;

                inPlayers.Remove(player);
                return;
            }

            // Remove them from the queue if they were in it
            if (queue.ContainsKey(player))
            {
                queue.Remove(player);
                updateQueue(queue);
            }
		}


		/// <summary>
		/// Called when the game begins
		/// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStarting = 0;

            //Clear out our dictionary of in players
            inPlayers.Clear();

            foreach (Player p in _arena.PlayersIngame)
            {
                inPlayers.Add(p, true);
                p.setVar("Hits", 0);
                p.setVar("Outs", 0);
                p.setVar("Catches", 0);
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
			_tickGameStarting = 0;
			Team _victoryTeam = null;

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

            _arena.sendArenaMessage(String.Format("&{0} are victorious with {1} player(s) left", _victoryTeam._name, pcount));

			return false;
		}

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Game.Breakdown")]
        public bool breakdown()
        {	//Allows additional "custom" per-arena breakdown information

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
                string format = "!3rd - (Hits={0} Outs={1} Catches={2}): {3}";

                switch (idx)
                {
                    case 2:
                        format = "!1st - (Hits={0} Outs={1} Catches={2}): {3}";
                        break;
                    case 1:
                        format = "!2nd - (Hits={0} Outs={1} Catches={2}): {3}";
                        break;
                }

                _arena.sendArenaMessage(String.Format(format, p.getVarInt("Hits"), p.getVarInt("Outs"), p.getVarInt("Catches"), p._alias));

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

            //Always return true;
            return true;
        }

        /// <summary>
		/// Creates a breakdown tailored for one player
		/// </summary>
   		[Scripts.Event("Player.Breakdown")]
		public bool individualBreakdown(Player from, bool bCurrent)
        {
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
                string format = "!3rd - (Hits={0} Outs={1} Catches={2}): {3}";

                switch (idx)
                {
                    case 2:
                        format = "!1st - (Hits={0} Outs={1} Catches={2}): {3}";
                        break;
                    case 1:
                        format = "!2nd - (Hits={0} Outs={1} Catches={2}): {3}";
                        break;
                }

                from.sendMessage(0, String.Format(format, p.getVarInt("Hits"), p.getVarInt("Outs"), p.getVarInt("Catches"), p._alias));
            }
            return true;
        }

		/// <summary>
		/// Called to reset the game state
		/// </summary>
		[Scripts.Event("Game.Reset")]
		public bool gameReset()
		{
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
            if (_arena.PlayersIngame.Count() >= _config.arena.playingMax)
            {
                enqueue(player);
                return false;
            }

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
        {   // Nothing to do here
            if (queue.Count == 0)
                return;

            if (_arena.PlayersIngame.Count() < _config.arena.playingMax)
            {
                Player nextPlayer = queue.ElementAt(0).Key;

                if (team1.ActivePlayerCount < _config.arena.maxPerFrequency)
                    nextPlayer.unspec(team1._name);
                else if (team2.ActivePlayerCount < _config.arena.maxPerFrequency)
                    nextPlayer.unspec(team2._name);

                queue.Remove(nextPlayer);
            }

            foreach (KeyValuePair<Player, int> player in queue)
            {
                queue[player.Key] = queue[player.Key] - 1;
                player.Key.sendMessage(0, String.Format("Queue position is now {0}", queue[player.Key]));
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
            if (inPlayers.ContainsKey(victim))
            {
                if (victim._team == team1)
                    team1Count--;
                else
                    team2Count--;


                killer.setVar("Hits", killer.getVarInt("Hits") + 1);
                victim.setVar("Outs", victim.getVarInt("Outs") + 1);
                inPlayers.Remove(victim);
            }
			return true;
		}
		#endregion
	}
}