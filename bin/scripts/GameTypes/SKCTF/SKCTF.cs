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

namespace InfServer.Script.GameType_SKCTF
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_SKCTF : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config
        private Tickets _tickets;               //Our tickets

        //Timers
        private int _lastGameCheck;             //The last time we polled the arena
        private int _tickGameStart;
        private int _tickGameStarting;
        private int _lastFlagCheck;

		//Settings
		private int _minPlayers;				//The minimum amount of players
        private int _ticketSmallChange;         //The small change to # of tickets (ex: kills, periodic flag rewards)


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

            _minPlayers = Int32.MaxValue;

			foreach (Arena.FlagState fs in _arena._flags.Values)
			{	//Determine the minimum number of players
				if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
					_minPlayers = fs.flag.FlagData.MinPlayerCount;

				//Register our flag change events
				fs.TeamChange += onFlagChange;
			}

            if (_minPlayers == Int32.MaxValue)
                //No flags? Run blank games
                _minPlayers = 1;

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
            }
            //Maybe the game is in progress...
            else
            {   //It is!

                //The change to small ticket changes needs to be updated based on players in game constantly
                _ticketSmallChange = (int)Math.Ceiling((double)25 / _arena.PlayersIngame.Count());

                //Let's update some points!
                int flagdelay = 2500; //1000 = 1 second
                if (now - _lastFlagCheck >= flagdelay)
                {   //It's time for a flag ticket update

                    //Loop through every flag
                    foreach (Arena.FlagState fs in _arena._flags.Values)
                        //Subtract tickets from every team that doesn't own the flag
                        foreach (Team t in _arena.DesiredTeams)
                            if (_arena.DesiredTeams.Contains(fs.team) && t != fs.team && _tickets != null)
                                _tickets[t] -= _ticketSmallChange;

                    //Update our tick
                    _lastFlagCheck = now;
                }
                updateTickers();
            }
            
			return true;
		}

		#region Events
		/// <summary>
		/// Called when a flag changes team
		/// </summary>
		public void onFlagChange(Arena.FlagState flag)
		{	//Does this team now have all the flags?
            bool allFlags = true;

            if(_arena._flags.Values.Count != 0)
			    foreach (Arena.FlagState fs in _arena._flags.Values)
                    if(fs.flag != null)
				        if (fs.team != flag.team)
					        allFlags = false;

            if (allFlags && _arena.DesiredTeams.Contains(flag.team))
                _arena.sendArenaMessage(flag.team._name + " controls all the flags!", 20);
		}

		/// <summary>
		/// Called when the specified team have won
		/// </summary>
		public void gameVictory(IEnumerable<Team> victors)
		{	//Game is over.
            //TODO: Calculate victory team rewards and rewards for all other players

            //Stop the game
            _arena.gameEnd();
		}

        /// <summary>
        /// Called when a teams tickets have been modified
        /// </summary>
        public void onTicketModify(Team team, int tickets)
        {
            //Update the tickers
            updateTickers();

            //Check for game victory here
            if (tickets <= 0)
                //They were the first team to lose all their tickets, they lose!
                gameVictory(_arena.DesiredTeams.Where(t => t != team));
        }

        public void updateTickers()
        {
            if (_tickets != null)
            {
                //Their teams tickets
                _arena.setTicker(0, 0, 0,
                    delegate(Player p)
                    {
                        //Update their ticker with current team ticket count
                        if(!_arena.DesiredTeams.Contains(p._team) && _tickets != null)
                            return "";
                        return "Your Team: " + _tickets[p._team];
                    }
                );
                //Other teams tickets
                _arena.setTicker(0, 1, 0,
                    delegate(Player p)
                    {
                        //Update their ticker with every other teams ticket count
                        List<string> otherTeams = new List<string>();
                        foreach (Team t in _arena.DesiredTeams)
                            if (t != p._team)
                                otherTeams.Add(t._name + ": " + _tickets[t]);

                        return String.Join(",", otherTeams.ToArray());
                    }
                );
                //Ticket costs
                _arena.setTicker(0, 2, 0, "Ticket costs: " + _ticketSmallChange);
            }
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
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

            //Scramble the teams!
            ScriptHelpers.scrambleTeams(_arena, 2, true);

			//Spawn our flags!
			_arena.flagSpawn();

            //Create some tickets and subscribe to our ticket modification event
            _tickets = null;
            _tickets = new Tickets(_arena.DesiredTeams, 1000);
            _tickets.TicketModify += onTicketModify;

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
            _tickets = null;
			_tickGameStart = 0;
			_tickGameStarting = 0;

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
            _tickets = null;
			_tickGameStart = 0;
			_tickGameStarting = 0;

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
            //Subtract a ticket from the victims team!
            if(_tickets != null)
                _tickets[victim._team] -= _ticketSmallChange;

            //Was it a computer kill?
            if (killType == Helpers.KillType.Computer)
            {
                //Let's find the vehicle!
                Computer cvehicle = victim._arena.Vehicles.FirstOrDefault(v => v._id == update.killerPlayerID) as Computer;
                Player vehKiller = cvehicle._creator;
                //Does it exist?
                if (cvehicle != null && vehKiller != null)
				{
                    //We'll take it from here...
                    update.type = Helpers.KillType.Player;
                    update.killerPlayerID = vehKiller._id;

                    //Don't reward for teamkills
                    if (vehKiller._team == victim._team)
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedTeam);
                    else
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedEnemy);

                    //Increase stats and notify arena of the kill!
                    vehKiller.Kills++;
                    victim.Deaths++;
                    Logic_Rewards.calculatePlayerKillRewards(victim, cvehicle._creator, update);
                    return false;
				}
            }
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