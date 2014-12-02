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

namespace InfServer.Script.GameType_Blank
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_Blank : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config

	



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

            return true;
        }



		#region Events
	
        /// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
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
		/// Called when a player enters the arena
		/// </summary>
		[Scripts.Event("Player.EnterArena")]
		public void playerEnterArena(Player player)
		{
		}

        /// <summary>
        /// Called when a player leaves the arena
        /// </summary>
        [Scripts.Event("Player.LeaveArena")]
        public void playerLeaveArena(Player player)
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
		{	//Game finished, perhaps start a new one



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
		{	
			return true;
		}

        /// <summary>
        /// Called when the player uses ?breakdown or end game is called
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool breakdown()
        {	//Allows additional "custom" breakdown information

            //Always return true;
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
        /// Triggered when a player dies to a bot
        /// </summary>
        [Scripts.Event("Player.BotKill")]
        public bool BotKill(Player victim, Bot killer)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a bot dies to a player
        /// </summary>
        [Scripts.Event("Player.BotDeath")]
        public bool BotDeath(Bot victim, Player killer, int weaponID)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player dies to a computer(E.G. turret)
        /// </summary>
        [Scripts.Event("Player.ComputerKill")]
        public bool ComputerKill(Player victim, Computer vehicle)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player buys from either shop or ?buy
        /// </summary>
        [Scripts.Event("Shop.Buy")]
        public bool PlayerShop(Player from, ItemInfo item, int quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player sells something either in the shop or ?sell
        /// </summary>
        [Scripts.Event("Shop.Sell")]
        public bool PlayerSell(Player from, ItemInfo item, int quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player requests to buy a skill
        /// </summary>
        [Scripts.Event("Shop.SkillRequest")]
        public bool PlayerShopSkillRequest(Player from, SkillInfo skill)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player's purchase is successful
        /// </summary>
        [Scripts.Event("Shop.SkillPurchase")]
        public bool PlayerShopSkillPurchase(Player from, SkillInfo skill)
        {
            //Return always true
            return true;
        }

        /// <summary>
        /// Triggered only when a special chat command is created here that isn't a server command.
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            return true;
        }

        /// <summary>
        /// Triggered only when a special mod command created here that isn't a server command.
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModcommand(Player player, Player recipient, string command, string payload)
        {
            //NOTE: DO NOT LEAVE AN EMPTY SCRIPT MOD COMMAND, IT WILL LOG IN DB
            //WITH ANYONE TYPING STUFF LIKE *HI
            //Return true if all requirements are met
            return false;
        }
		#endregion
	}
}