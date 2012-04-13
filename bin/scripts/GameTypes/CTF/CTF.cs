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

namespace InfServer.Script.GameType_CTF
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_CTF : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config

		private int _jackpot;					//The game's jackpot so far

		private Team _victoryTeam;				//The team currently winning!
		private int _tickVictoryStart;			//The tick at which the victory countdown began
		private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
		private int _victoryNotice;				//The number of victory notices we've done

		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;				//The tick at which the game started (0 == stopped)

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

			_minPlayers = 1;

			foreach (Arena.FlagState fs in _arena._flags.Values)
			{	//Determine the minimum number of players
				if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
					_minPlayers = fs.flag.FlagData.MinPlayerCount;

				//Register our flag change events
				fs.TeamChange += onFlagChange;
			}

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
			//Do we have enough players to start a game?
			else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
			{	//Great! Get going
				_tickGameStarting = now;
				_arena.setTicker(1, 1, _config.flag.startDelay * 100, "Next game: ",
					delegate()
					{	//Trigger the game start
						_arena.gameStart();
					}
				);

				//There will be a game soon, trigger the event
				/*string soonGame = _server._zoneConfig.EventInfo.soonGame;
				foreach (Player player in _players)
					if (!player.IsSpectator)
						Logic_Assets.RunEvent(player, soonGame);*/
			}

			//Is anybody experiencing a victory?
			if (_tickVictoryStart != 0)
			{	//Have they won yet?
				if (now - _tickVictoryStart > (_config.flag.victoryHoldTime * 10))
					//Yes! Trigger game victory
					gameVictory(_victoryTeam);
				else
				{	//Do we have a victory notice to give?
					if (_tickNextVictoryNotice != 0 && now > _tickNextVictoryNotice)
					{	//Yes! Let's give it
						int countdown = (_config.flag.victoryHoldTime / 100) - ((now - _tickVictoryStart) / 1000);
						_arena.sendArenaMessage(String.Format("Victory for {0} in {1} seconds!",
							_victoryTeam._name, countdown), _config.flag.victoryWarningBong);

						//Plan the next notice
						_tickNextVictoryNotice = _tickVictoryStart;
						_victoryNotice++;

						if (_victoryNotice == 1 && countdown >= 30)
							//Default 2/3 time
							_tickNextVictoryNotice += (_config.flag.victoryHoldTime / 3) * 10;
						else if (_victoryNotice == 2 || (_victoryNotice == 1 && countdown >= 20))
							//10 second marker
							_tickNextVictoryNotice += (_config.flag.victoryHoldTime * 10) - 10000;
						else
							_tickNextVictoryNotice = 0;
					}
				}
			}

			return true;
		}

		#region Events
		/// <summary>
		/// Called when a flag changes team
		/// </summary>
		public void onFlagChange(Arena.FlagState flag)
		{	//Does this team now have all the flags?
			Team victoryTeam = flag.team;

			foreach (Arena.FlagState fs in _arena._flags.Values)
				if (fs.bActive && fs.team != victoryTeam)
					victoryTeam = null;

			if (victoryTeam != null)
			{	//Yes! Victory for them!
				_arena.setTicker(1, 1, _config.flag.victoryHoldTime, "Victory in ");
				_tickNextVictoryNotice = _tickVictoryStart = Environment.TickCount;
				_victoryTeam = victoryTeam;
			}
			else
			{	//Aborted?
				if (_victoryTeam != null)
				{
					_tickVictoryStart = 0;
                    _tickNextVictoryNotice = 0;
					_victoryTeam = null;

					_arena.sendArenaMessage("Victory has been aborted.", _config.flag.victoryAbortedBong);
					_arena.setTicker(1, 1, 0, "");
				}
			}
		}

		/// <summary>
		/// Called when the specified team have won
		/// </summary>
		public void gameVictory(Team victors)
		{	//Let everyone know
			if (_config.flag.useJackpot)
				_jackpot = (int)Math.Pow(_arena.PlayerCount, 2);            

			_arena.sendArenaMessage(String.Format("Victory={0} Jackpot={1}", victors._name, _jackpot), _config.flag.victoryBong);            

            //TODO: Move this calculation to breakdown() in ScriptArena?
			//Calculate the jackpot for each player
            foreach (Player p in _arena.Players)
            {	//Spectating? Psh.
                if (p.IsSpectator)
                    continue;
                //Find the base reward
                int personalJackpot;

                if (p._team == victors)
                    personalJackpot = _jackpot * (_config.flag.winnerJackpotFixedPercent / 1000);
                else
                    personalJackpot = _jackpot * (_config.flag.loserJackpotFixedPercent / 1000);

                //Obtain the respective rewards
                int cashReward = personalJackpot * (_config.flag.cashReward / 1000);
                int experienceReward = personalJackpot * (_config.flag.experienceReward / 1000);
                int pointReward = personalJackpot * (_config.flag.pointReward / 1000);

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }

            //Stop the game
            _arena.gameEnd();
		}

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower() == "test")
            {
                player.sendMessage(0, "Test");
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
		/// Called when a player leaves the game
		/// </summary>
		[Scripts.Event("Player.Leave")]
		public void playerLeave(Player player)
		{
		}

		/// <summary>
		/// Called when the game begins
		/// </summary>
		[Scripts.Event("Game.Start")]
		public bool gameStart()
		{	//We've started!
			_tickGameStart = Environment.TickCount;
			_tickGameStarting = 0;

			//Spawn our flags!
			_arena.flagSpawn();

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
			_tickVictoryStart = 0;
			_tickNextVictoryNotice = 0;
			_victoryTeam = null;

			return true;
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
			_tickVictoryStart = 0;
			_tickNextVictoryNotice = 0;

			_victoryTeam = null;

			return true;
		}

		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		[Scripts.Event("Player.ItemPickup")]
		public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
		{	
			return true;
		}

		/// <summary>
		/// Triggered when a player requests to drop an item
		/// </summary>
		[Scripts.Event("Player.ItemDrop")]
		public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
		{
			return true;
		}

		/// <summary>
		/// Handles a player's portal request
		/// </summary>
		[Scripts.Event("Player.Portal")]
		public bool playerPortal(Player player, LioInfo.Portal portal)
		{           
            List<Arena.FlagState> carried = _arena._flags.Values.Where(flag => flag.carrier == player).ToList();
            
            foreach (Arena.FlagState carry in carried)
            {   //If the terrain number is 0-15

                int terrainNum = player._arena.getTerrainID(player._state.positionX, player._state.positionY);                
                if (terrainNum >= 0 && terrainNum <= 15)
                {   //Check the FlagDroppableTerrains for that specific terrain id
                    
                    if (carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0)
                        _arena.flagResetPlayer(player);
                }
            }
             
			return true;
		}

		/// <summary>
		/// Handles a player's produce request
		/// </summary>
		[Scripts.Event("Player.Produce")]
		public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
		{
			return true;
		}

		/// <summary>
		/// Handles a player's switch request
		/// </summary>
		[Scripts.Event("Player.Switch")]
		public bool playerSwitch(Player player, LioInfo.Switch swi)
		{
			return true;
		}

		/// <summary>
		/// Handles a player's flag request
		/// </summary>
		[Scripts.Event("Player.FlagAction")]
		public bool playerFlagAction(Player player, bool bPickup, bool bInPlace, LioInfo.Flag flag)
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
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to spec and leave the game
		/// </summary>
		[Scripts.Event("Player.LeaveGame")]
		public bool playerLeaveGame(Player player)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to enter a vehicle
		/// </summary>
		[Scripts.Event("Player.EnterVehicle")]
		public bool playerEnterVehicle(Player player, Vehicle vehicle)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to leave a vehicle
		/// </summary>
		[Scripts.Event("Player.LeaveVehicle")]
		public bool playerLeaveVehicle(Player player, Vehicle vehicle)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
		[Scripts.Event("Player.Explosion")]
		public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
		{
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
			return true;
		}

		/// <summary>
		/// Triggered when a bot has killed a player
		/// </summary>
		[Scripts.Event("Player.BotKill")]
		public bool playerBotKill(Player victim, Bot bot)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a computer vehicle has killed a player
		/// </summary>
		[Scripts.Event("Player.ComputerKill")]
		public bool playerComputerKill(Player victim, Computer computer)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.WarpItem")]
		public bool playerWarpItem(Player player, ItemInfo.WarpItem item, ushort targetPlayerID, short posX, short posY)
		{                
			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.MakeVehicle")]
		public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.MakeItem")]
		public bool playerMakeItem(Player player, ItemInfo.ItemMaker item, short posX, short posY)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player is buying an item from the shop
		/// </summary>
		[Scripts.Event("Shop.Buy")]
		public bool shopBuy(Player patron, ItemInfo item, int quantity)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player is selling an item to the shop
		/// </summary>
		[Scripts.Event("Shop.Sell")]
		public bool shopSell(Player patron, ItemInfo item, int quantity)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a vehicle is created
		/// </summary>
		/// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
		[Scripts.Event("Vehicle.Creation")]
		public bool vehicleCreation(Vehicle created, Team team, Player creator)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		[Scripts.Event("Vehicle.Death")]
		public bool vehicleDeath(Vehicle dead, Player killer)
		{
			return true;
		}
		#endregion
	}
}