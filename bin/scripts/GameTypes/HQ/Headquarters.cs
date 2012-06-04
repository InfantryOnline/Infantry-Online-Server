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

namespace InfServer.Script.GameType_HQ
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	public class Script_HQ : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		public Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config
        public static Dictionary<Team, HQ> _hqs = new Dictionary<Team, HQ>();       //Our list of HQs
        private int tickLastUpdate;


        ///////////////////////////////////////////////////
        // Headquarter Configuration
        ///////////////////////////////////////////////////

        //Timing
        private int rewardDelay = 90;                  //Periodic reward delay (in seconds)

        //Rewards
        public static int baseCash = 150;                   //Base cash reward
        public static int baseExp = 80;                     //Base experience reward
        public static int basePoints = 175;                 //Base point reward
        public static int baseBountyPerKill = 25;           //Bounty rewarded per kill for HQ
        public static double vehicleKillMultiply = 1.25;    //Bounty multiplier for vehicle kills

        public static double cashMultiplier = 2;            //Multipliers for rewarding those who destroy a HQ
        public static double expMultipler = 1.50;
        public static double pointMultiplier = 2.25;

        //Leveling
        public static double baseMultiplier = 1.75;         //Base Bounty multipler for scaling Bounty required to level an HQ.
        public static int baseBounty = 500;                 //Base Bounty required to level an HQ.
        public static int levelHump = 10000;                //Base amount of bounty added to certain level humps

        //Other
        public static int killRadius = 3000;                //Radius in which kills count for HQ bounty.
        public static int killRadiusPylon = 1500;           //Radius in which kills count for HQ bounty (Pylon Extension)
        public static int doubleXP = 1;                     //Double xp Modifier, 1 = false 2 = true.





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

			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
		public bool poll()
		{	//Should we check game state yet?
			int now = Environment.TickCount;

            //TODO: Refactor this... wrong way of maintainings reward and levels
            foreach (HQ hq in _hqs.Values)
            {
                //Reward time?
                if ((now - tickLastUpdate) > (rewardDelay * 1000))
                {
                    Events.periodicReward(hq);
                    //Last HQ in line
                    if (hq == _hqs.Last().Value)
                        tickLastUpdate = now;
                }
                //Level up time?
                if ((now - tickLastUpdate) > 1000)
                    if (hq.bounty >= hq.nextLvl)
                        Events.onHQLevelUp(hq);
            }

			return true;
		}

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {

            if (command.ToLower() == "hqlist")
            {
                foreach (HQ hq in _hqs.Values)
                {
                    player.sendMessage(0, 
                        String.Format("[HQ] (Team={0} Bounty={1} Location={2})", 
                        hq.team._name, 
                        hq.bounty,
                        Helpers.posToLetterCoord(hq.vehicle._state.positionX, hq.vehicle._state.positionY)));
                }
            }

            else if (command.ToLower() == "hq")
            {
                if (!_hqs.Keys.Contains(player._team))
                {
                    player.sendMessage(-1, "No Headquarters");
                    return false;
                }

                HQ hq = _hqs[player._team];

                player.sendMessage(0, "~[HQ] - Information!");
                player.sendMessage(0, String.Format("[HQ] - Level: {0}", hq.level));
                player.sendMessage(0, String.Format("[HQ] - Next Level: {0}", hq.nextLvl));
                player.sendMessage(0, String.Format("[HQ] - Total Bounty: {0}", hq.bounty));
                player.sendMessage(0, String.Format("[HQ] - Location: {0}", 
                    Helpers.posToLetterCoord(hq.vehicle._state.positionX, hq.vehicle._state.positionY)));


            }

            else if (command.ToLower() == "bounty")
            {
                if (player.PermissionLevel > 0)
                {
                    if (_hqs.ContainsKey(player._team))
                    {
                        _hqs[player._team].bounty += Int32.Parse(payload);
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
		{
			return true;
		}

		/// <summary>
		/// Called when the game ends
		/// </summary>
		[Scripts.Event("Game.End")]
		public bool gameEnd()
		{
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
            //Calculate rewards AGAIN if double exp
            if (doubleXP == 2 && killer != null)
            {
                Logic_Rewards.calculatePlayerKillRewards(victim, killer, update);
            }
			return true;
		}

		/// <summary>
		/// Triggered when one player has killed another
		/// </summary>
		[Scripts.Event("Player.PlayerKill")]
		public bool playerPlayerKill(Player victim, Player killer)
		{
            if (_hqs.Keys.Contains(killer._team))
            {
                HQ headq = _hqs[killer._team];

                List<Vehicle> inRange = _arena.getVehiclesInRange(
                    killer._state.positionX, killer._state.positionY,
                    killRadius);
                //Blasphemy!
                if (killer._team == victim._team)
                    return false;

                //Is it in range?
                if (inRange.Contains(headq.vehicle))
                {
                    Events.onPlayerKill(headq, killer, victim);
                    return true;
                }
                //Pylon?
                if (headq.pylon != null && inRange.Contains(headq.pylon))
                {
                    Events.onPlayerKill(headq, killer, victim);
                    return true;
                }
            }
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
            if (_hqs.Keys.Contains(computer._team))
            {
                //Smaller range for Computers
                List<Vehicle> hqsInRange = _arena.getVehiclesInRange(computer._state.positionX, computer._state.positionY, 1000);

                //Is it in range?
                if (hqsInRange.Contains(_hqs[computer._team].vehicle))
                {
                    HQ headq = _hqs[computer._team];
                    Events.onComputerKill(headq, computer, victim);
                }
            }
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
            //HQ?
            if (created._type.Id == 463)
            {
                //Already have one?
                if (_hqs.Keys.Contains(team))
                {
                    creator.sendMessage(-1, "Your team already has a headquarters");
                    created.destroy(false, true);
                    return false;
                }
                //No
                else
                {
                    //Create it
                    HQ newHQ = new HQ(created);
                    _hqs.Add(team, newHQ);
                }
            }
            else if (created._type.Id == 480)
            {
                if (_hqs.Keys.Contains(team))
                {
                    HQ headQ = _hqs[team];
                    headQ.pylon = created;
                }
            }
			return true;
		}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		[Scripts.Event("Vehicle.Death")]
		public bool vehicleDeath(Vehicle dead, Player killer)
		{
            //HeadQuarters?
            if (dead._type.Id == 463)
            {
                if (_hqs.Keys.Contains(dead._team))
                {
                    HQ headQ = _hqs[dead._team];

                    if (headQ.team != killer._team)
                    {
                        Events.onHQDeath(headQ, killer);
                    }
                    else
                    {
                        _arena.sendArenaMessage(
                            String.Format("~[HQ] - Oops! - Team {0} killed their own headquarters, Silly!", headQ.team._name)); 
                    }

                    _hqs.Remove(dead._team);
                    
                }
            }
            //Pylon?
            else if (dead._type.Id == 480)
            {
                if (_hqs.Keys.Contains(dead._team))
                {
                    HQ headQ = _hqs[dead._team];
                    headQ.pylon = null;
                }
            }

            //Car?
            if (dead._type.Type == VehInfo.Types.Car)
            {

                if (_hqs.Keys.Contains(killer._team))
                {
                    HQ headq = _hqs[killer._team];

                    List<Vehicle> inRange = _arena.getVehiclesInRange(
                        killer._state.positionX, killer._state.positionY,
                        killRadius);
                    //Blasphemy!
                    if (killer._team == dead._team)
                        return false;

                    //Is it in range?
                    if (inRange.Contains(headq.vehicle))
                    {
                        Events.onVehicleKill(headq, killer, dead);
                        return true;
                    }
                    //Pylon?
                    if (headq.pylon != null && inRange.Contains(headq.pylon))
                    {
                        Events.onVehicleKill(headq, killer, dead);
                        return true;
                    }

                }
            }
			return true;
		}
	}
}