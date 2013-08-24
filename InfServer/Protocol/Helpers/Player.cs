using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	// Member Classes
		//////////////////////////////////////////////////
		//Type of object update
		public enum Update_Type
		{
			Car = 5,
		}

		public enum ResetFlags
		{
			ResetNone = 0,
			ResetEnergy = 1,
			ResetHealth = 2,
			ResetVelocity = 4,
			ResetAll = 7,
		};

		//Type of chat
		public enum Chat_Type
		{
			Normal = 0,
            Macro = 1,
			Whisper = 2,
			Team = 3,
			EnemyTeam = 4,
			Arena = 5,
            PrivateChat = 6,
			Squad = 7
		}

		//Ways in which players can be killed
		public enum KillType
		{
			Player,
			Computer,
			Terrain,
			Flag,
			Prize,
			Explosion,
		}

		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Notifies a player of a player kill
		/// </summary>
		static public void Player_RouteKill(
			Player p, CS_VehicleDeath death, Player dead,
			int cash, int points, int personalPoints, int experience)
		{	//Prepare a death packet
			SC_VehicleDeath not = new SC_VehicleDeath();

			not.type = death.type;

			not.playerID = dead._id;
			not.vehicleID = (dead._occupiedVehicle == null ? dead._id : dead._occupiedVehicle._id);
			not.killerID = death.killerPlayerID;
			not.positionX = death.positionX;
			not.positionY = death.positionY;
			not.yaw = dead._state.yaw;

			not.points = (short)points;
			not.personalCash = (short)cash;
			not.personalPoints = (short)personalPoints;
			not.personalExp = (short)experience;

			//Send it to each player
			p._client.sendReliable(not);
		}

		/// <summary>
		/// Notifies a group of players of a player kill
		/// </summary>
		static public void Player_RouteKill(
			IEnumerable<Player> p, CS_VehicleDeath death, Player dead,
			int cash, int points, int personalPoints, int experience)
		{	//Prepare a death packet
			SC_VehicleDeath not = new SC_VehicleDeath();

			not.type = death.type;

			not.playerID = dead._id;
			not.vehicleID = (dead._occupiedVehicle == null ? dead._id : dead._occupiedVehicle._id);
			not.killerID = death.killerPlayerID;
			not.positionX = death.positionX;
			not.positionY = death.positionY;
			not.yaw = dead._state.yaw;

			not.points = (short)points;
			not.personalCash = (short)cash;
			not.personalPoints = (short)personalPoints;
			not.personalExp = (short)experience;

			//Send it to each player
			foreach (Player player in p)
				player._client.sendReliable(not);
		}

		/// <summary>
		/// Sets a player's team
		/// </summary>
		static public void Player_SetTeam(IEnumerable<Player> players, Player p, Team t)
		{	//Prepare a team change packet
			SC_ChangeTeam ct = new SC_ChangeTeam();

			ct.playerID = p._id;
			ct.teamID = t._id;
			ct.teamname = t._name;

			//Send it to each player
			foreach (Player player in players)
				player._client.sendReliable(ct);
		}

		/// <summary>
		/// Updates the player's inventory
		/// </summary>
		static public void Player_InventoryUpdate(Player p)
		{	//Construct and send the appropriate packet
			SC_Inventory inv = new SC_Inventory();

			inv.cash = p.Cash;
			inv.inventoryCount = p._inventory.Count;
			inv.inventory = p._inventory.Values;

			p._client.sendReliable(inv);
		}

		/// <summary>
		/// Initializes the player's state
		/// </summary>
		static public void Player_StateInit(Player p, Action completionCallback)
		{	//Formulate the stats init packet
			SC_PlayerState si = new SC_PlayerState();

			si.cash = p.Cash;
			si.experienceRemaining = p.Experience;
			si.experience = p.ExperienceTotal;

			si.inventoryCount = p._inventory.Count;
			si.inventory = p._inventory.Values;
			si.skillCount = p._skills.Count;
			si.skills = p._skills.Values;

            p._client.sendReliable(si, completionCallback);
            
		}

		/// <summary>
		/// Provides an easy means of routing chat messages between players
		/// </summary>
		static public void Player_RouteChat(Player p, Player from, CS_Chat chat)
		{	//Formulate the chat notification!
			SC_Chat schat = new SC_Chat();

			schat.chatType = chat.chatType;

			schat.from = from._alias;
			schat.message = chat.message;
            schat.bong = chat.bong;

			//Go!
			p._client.sendReliable(schat);
		}

        /// <summary>
        /// Provides an easy means of routing chat messages between players
        /// </summary>
        static public void Player_RouteChat(Player p, Player from, SC_Whisper<Data.Database> chat)
        {	//Formulate the chat notification!
            SC_Chat schat = new SC_Chat();

            schat.chatType = Chat_Type.Whisper;

            schat.from = chat.from;
            schat.message = chat.message;
            schat.bong = chat.bong;

            //Go!
            p._client.sendReliable(schat);
        }



        /// <summary>
        /// Provides an easy means of routing chat messages between players
        /// </summary>
        static public void Player_RouteChat(Player p, SC_PrivateChat<Data.Database> pkt)
        {	//Formulate the chat notification!
            SC_Chat schat = new SC_Chat();

            schat.chatType = Chat_Type.PrivateChat;
            schat.message = pkt.message;
            schat.from = pkt.from;

            //Go!
            p._client.sendReliable(schat);
        }

		/// <summary>
		/// Like the above, but sends messages to multiple players
		/// </summary>
		static public void Player_RouteChat(IChatTarget target, Player from, CS_Chat chat)
		{	//Formulate the chat notification!
			SC_Chat schat = new SC_Chat();

			schat.chatType = chat.chatType;

			schat.from = from._alias;
			schat.message = chat.message;
            schat.bong = chat.bong;

			//Go!
			foreach (Player player in target.getChatTargets())
				if (player != from)
					player._client.sendReliable(schat);
		}

		/// <summary>
		/// Notifies a player that he's been disconnected
		/// </summary>
		static public void Player_Disconnect(Player p)
		{	//Formulate our message!
			Disconnect discon = new Disconnect();

			discon.connectionID = p._client._connectionID;
			discon.reason = Disconnect.DisconnectReason.DisconnectReasonApplication;

			p._client.send(discon);
		}

		/// <summary>
		/// Notifies a player that the shop request has been processed
		/// </summary>
		static public void Player_ShopFinish(Player p)
		{	//Unf
			p._client.sendReliable(new SC_ShopFinished());
		}

		/// <summary>
		/// Clears all active projectiles
		/// </summary>
		static public void Player_ClearProjectiles(Player p)
		{
			p._client.sendReliable(new SC_ClearProjectiles());
		}

		/// <summary>
		/// Activates a multiitem for the client
		/// </summary>
		static public void Player_UseMultiItems(Player p, short itemid, short itemcount)
		{
			SC_PlayerMultiItem multi = new SC_PlayerMultiItem();

			multi.multiItemID = itemid;
			multi.itemCount = itemcount;

			p._client.sendReliable(multi);
		}

		/// <summary>
		/// Sets a player's bounty
		/// </summary>
		static public void Player_SetBounty(Player p, short bounty)
		{
			SC_PlayerSetBounty bty = new SC_PlayerSetBounty();

			bty.playerID = p._id;
			bty.bounty = bounty;

			p._client.sendReliable(bty);
		}

		/// <summary>
        /// Prompts a player to start spectating another player
		/// </summary>
		/// <param name="stop">defaults to false. if set to true, will stop spectating player</param>
		static public void Player_SpectatePlayer(Player p, Player toSpec, bool stop=false)
		{	//Form the packet
			SC_PlayerSpectate pkt = new SC_PlayerSpectate();

			pkt.spectatorID = p._id;
            if (!stop)
                pkt.playerID = toSpec._id;
            else
                pkt.playerID = ushort.MinValue;

			pkt.unk1 = -1;
			pkt.unk2 = 1;

			p._client.sendReliable(pkt);
		}

		/// <summary>
		/// Provides an easy means of routing item used notifications between players
		/// </summary>
		static public void Player_RouteItemUsed(Player p, Player from, UInt16 targetVehicle, Int16 itemID, Int16 posX, Int16 posY, byte yaw)
		{	//Create the item used packet
			SC_ItemUsed used = new SC_ItemUsed();

			used.userPlayer = from._id;
			used.targetVehicle = targetVehicle;
			used.itemID = itemID;
			used.posX = posX;
			used.posY = posY;
			used.yaw = yaw;

			//Go!
			p._client.sendReliable(used);
		}

		/// <summary>
		/// Provides an easy means of routing item used notifications to multiple players
		/// </summary>
		static public void Player_RouteItemUsed(bool bSkipSelf, IEnumerable<Player> players, Player from, UInt16 targetVehicle, Int16 itemID, Int16 posX, Int16 posY, byte yaw)
		{	//Create the item used packet
			SC_ItemUsed used = new SC_ItemUsed();

			used.userPlayer = from._id;
			used.targetVehicle = targetVehicle;
			used.itemID = itemID;
			used.posX = posX;
			used.posY = posY;
			used.yaw = yaw;

			//Go!
			foreach (Player player in players)
				if (!bSkipSelf || player != from)
					player._client.sendReliable(used);
		}

        /// <summary>
        /// Provides an easy means of routing an explosion to multiple players
        /// </summary>
        static public void Player_RouteExplosion(IEnumerable<Player> players, Int16 itemID, Int16 posX, Int16 posY, Int16 posZ, byte yaw, ushort creator)
        {
            SC_Projectile explosion = new SC_Projectile();

            explosion.projectileID = itemID;
            explosion.playerID = creator;
            explosion.posX = posX;
            explosion.posY = posY;
            explosion.posZ = posZ;
            explosion.yaw = yaw;

            //Route the explosion
            foreach (Player p in players)
                p._client.sendReliable(explosion);
        }

        /// <summary>
        /// Give or take crowns from a list of people
        /// </summary>
        static public void Player_Crowns(Arena arena, bool bCrown, List<Player> players, Player target)
        {
            SC_PlayerCrowns pkt = new SC_PlayerCrowns();
            pkt.bCrown = bCrown;
            pkt.players = players.Select(p => (short)p._id).ToList();

            if (target == null)
                foreach (Player p in arena.Players)
                    p._client.sendReliable(pkt);
            else
                target._client.sendReliable(pkt);
        }

        static public void Player_Crowns(Arena arena, bool bCrown, List<Player> players)
        {
            Player_Crowns(arena, bCrown, players, null);
        }
	}
}
