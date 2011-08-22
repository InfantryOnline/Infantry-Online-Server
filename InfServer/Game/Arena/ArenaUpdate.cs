using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Logic;
using InfServer.Bots;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public abstract partial class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		#region Update
		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		public virtual void handlePlayerPickup(Player from, CS_PlayerPickup update)
		{	}

		/// <summary>
		/// Triggered when a player requests to drop an item
		/// </summary>
		public virtual void handlePlayerDrop(Player from, CS_PlayerDrop update)
		{	}

		/// <summary>
		/// Handles a player's portal request
		/// </summary>
		public virtual void handlePlayerPortal(Player from, LioInfo.Portal portal)
		{	}

		/// <summary>
		/// Handles a player's produce request
		/// </summary>
		public virtual void handlePlayerProduce(Player from, ushort computerVehID, ushort produceItem)
		{ }

		/// <summary>
		/// Handles a player's switch request
		/// </summary>
		public virtual void handlePlayerSwitch(Player from, bool bOpen, LioInfo.Switch swi)
		{	}

		/// <summary>
		/// Handles a player's flag request
		/// </summary>
		public virtual void handlePlayerFlag(Player from, bool bPickup, bool bInPlace, LioInfo.Flag flag)
		{	}

		/// <summary>
		/// Handles the spawn of a player
		/// </summary>
		public virtual void handlePlayerSpawn(Player from, bool bDeath)
		{	}

		/// <summary>
		/// Triggered when a player wants to spec or unspec
		/// </summary>
		public virtual void handlePlayerJoin(Player from, bool bSpec)
		{	}

		/// <summary>
		/// Triggered when a player wants to enter a vehicle
		/// </summary>
		public virtual void handlePlayerEnterVehicle(Player from, bool bEnter, ushort vehicleID)
		{	}

		/// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
		public virtual void handlePlayerExplosion(Player from, CS_Explosion update)
		{	}

		/// <summary>
		/// Triggered when a player has sent an update packet
		/// </summary>
		public virtual void handlePlayerUpdate(Player from, CS_PlayerUpdate update)
		{	}

		/// <summary>
		/// Triggered when a player has sent a death packet
		/// </summary>
		public virtual void handlePlayerDeath(Player from, CS_VehicleDeath update)
		{	}

		/// <summary>
		/// Triggered when a player attempts to use the store
		/// </summary>
		public virtual void handlePlayerShop(Player from, ItemInfo item, int quantity)
		{	}

		/// <summary>
		/// Triggered when a player attempts to use the skill shop
		/// </summary>
		public virtual void handlePlayerShopSkill(Player from, SkillInfo skill)
		{	}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		public virtual void handlePlayerWarp(Player player, ItemInfo.WarpItem item, ushort targetPlayerID, short posX, short posY)
		{	}

		/// <summary>
		/// Triggered when a player attempts to use a vehicle creator
		/// </summary>
		public virtual void handlePlayerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
		{	}

		/// <summary>
		/// Triggered when a player's item expires
		/// </summary>
		public virtual void handlePlayerItemExpire(Player player, ushort itemTypeID)
		{	}

		/// <summary>
		/// Triggered when a player attempts to use an item creator
		/// </summary>
		public virtual void handlePlayerMakeItem(Player player, ItemInfo.ItemMaker item, short posX, short posY)
		{	}

        /// <summary>
        /// Triggered when a player attempts to repair(heal)
        /// </summary>
        public virtual void handlePlayerRepair(Player player, ItemInfo.RepairItem item, UInt16 targetVehicle, short posX, short posY)
        {   }

		/// <summary>
		/// Triggered when a player attempts to spectate another player
		/// </summary>
		public virtual void handlePlayerSpectate(Player player, ushort targetPlayerID)
		{	}

		/// <summary>
		/// Triggered when a vehicle is created
		/// </summary>
		/// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
		public virtual void handleVehicleCreation(Vehicle created, Team team, Player creator)
		{	}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		public virtual void handleVehicleDeath(Vehicle dead, Player killer, Player occupier)
		{	}

		/// <summary>
		/// Triggered when a bot is killed
		/// </summary>
		public virtual void handleBotDeath(Bot dead, Player killer, int weaponID)
		{ }

		#endregion
	}
}
