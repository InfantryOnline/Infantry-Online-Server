using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;
using InfServer.Bots;


namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Provides an easy means of routing computer vehicle update messages
		/// </summary>
		static public void Update_RouteComputer(IEnumerable<Player> p, Computer update)
		{	//Prepare the appropriate packets
			SC_PlayerTeamUpdate tu = new SC_PlayerTeamUpdate();

			tu.player = null;
			tu.vehicle = update;
			tu.itemID = (short)(update._shouldFire ? update._primaryGun.id : -1);			

			tu.activeEquip = new List<ushort>();

			/*//Prepare the appropriate packets
			SC_PlayerUpdate up = new SC_PlayerUpdate();

			up.updateType = Update_Type.Car;
			up.playerID = 0xFFFF;
			up.tickCount = (short) Environment.TickCount;
			up.itemID = update._shouldFire ? (short) update._primaryGun.id : (short) -1;
			up.vehicleID = update._id;
			up.health = update._state.health;
			up.velocityX = update._state.velocityX;
			up.velocityY = update._state.velocityY;
			up.velocityZ = update._state.velocityZ;
			up.positionX = update._state.positionX;
			up.positionY = update._state.positionY;
			up.positionZ = update._state.positionZ;
			up.yaw = update._state.yaw;
			up.direction = update._state.direction;
			up.unk3 = update._state.unk1;
			up.activeEquip = new List<ushort>(0);*/

			//Send our updates..
			foreach (Player player in p)
				player._client.send(tu);				

			// Disable fire
			update._shouldFire = false;
			update._sendUpdate = false;
		}

		/// <summary>
		/// Provides an easy means of routing movement update messages between players
		/// </summary>
		static public void Update_RoutePlayer(IEnumerable<Player> p, Player update, CS_PlayerUpdate pkt)
		{	//Prepare the appropriate packets
			SC_PlayerTeamUpdate tu = new SC_PlayerTeamUpdate();

			tu.player = update;
			tu.vehicle = update.ActiveVehicle;
			tu.itemID = pkt.itemID;
			
			tu.activeEquip = pkt.activeEquip;

			SC_PlayerUpdate up = new SC_PlayerUpdate();

			up.updateType = Update_Type.Car;
			up.playerID = update._id;
			up.tickCount = (short)Environment.TickCount;
			up.itemID = (pkt.itemID == 0 ? (short)-1 : pkt.itemID);
			up.vehicleID = (update._occupiedVehicle == null) ? update._id : update._occupiedVehicle._id;
			up.health = update._state.health;
			up.velocityX = update._state.velocityX;
			up.velocityY = update._state.velocityY;
			up.velocityZ = update._state.velocityZ;
			up.positionX = update._state.positionX;
			up.positionY = update._state.positionY;
			up.positionZ = update._state.positionZ;
			up.yaw = update._state.yaw;
			up.direction = (UInt16)update._state.direction;
			up.unk3 = update._state.unk1;
			up.activeEquip = pkt.activeEquip;
			
			//Send our updates..
			foreach (Player player in p)
			{	//Don't send duplicates
				if (player == update)
					continue;

				//if (player._team == update._team)
					player._client.send(tu);
				//else
				//	player._client.send(up);
			}
		}

		/// <summary>
		/// Provides an easy means of routing bot update packets to players
		/// </summary>
		static public void Update_RouteBot(IEnumerable<Player> p, Bot update)
		{	//Prepare the appropriate packets
			SC_PlayerTeamUpdate tu = new SC_PlayerTeamUpdate();

			tu.player = null;
			tu.vehicle = update;
			tu.itemID = (short)update._itemUseID;
			tu.bBot = true;

			//Send our updates..
			foreach (Player player in p)
			{
				//if (player._team == update._team)
				player._client.send(tu);
				//else
				//	player._client.send(up);
			}
		}
	}
}
