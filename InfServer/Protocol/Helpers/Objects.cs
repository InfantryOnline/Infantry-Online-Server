using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;
using Axiom.Math;

namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	// Member Classes
		//////////////////////////////////////////////////
		/// <summary>
		/// Contains positional and other information an object
		/// </summary>
		public class ObjectState
		{
			public Int16 health;
			public Int16 energy;

			public Int16 velocityX;			//Velocity info
			public Int16 velocityY;			//
			public Int16 velocityZ;			//

			public Int16 positionX;			//Positional info
			public Int16 positionY;			//
			public Int16 positionZ;			//

			public byte yaw;				//Our rotation
			public Direction direction;		//The direction we're attempting to move in
			public byte unk1;				//Unknown (flags?)

			public byte pitch;

			//
			public byte fireAngle;			//Used for computer vehicles
			public int lastUpdate;			//The last point this state was updated
			public int lastUpdateServer;	//The time at which this was received on the server
			public int updateNumber;		//The update counter, used for route range factoring

			//Assume the direction system is hardcoded for now
			public enum Direction : ushort
			{
				None = 0,
				Forward = 0x3C,
				Backward = 0xD3,
				StrafeLeft = 0xBF00,
				StrafeRight = 0x4100,
				NorthWest = 0xD32A,
				SouthWest = 0xD3E1,
				SouthEast = 0x2DE1,
				NorthEast = 0x2D2A,
			}

			//Converting objects to vector space
			public Vector3 velocity()
			{
				return new Vector3(((float)velocityX) / 1000, ((float)velocityY) / 1000, ((float)velocityZ) / 1000);
			}

			public Vector3 position()
			{
				if (lastUpdate != 0)
				{	//We want to find the player's -current- position
					double timeElapsed = (Environment.TickCount - lastUpdate) / 1000.0d;

					return new Vector3(	(((float)positionX) + ((((float)velocityX) / 10) * timeElapsed)) / 100,
										(((float)positionY) + ((((float)velocityY) / 10) * timeElapsed)) / 100,
										(((float)positionZ) + ((((float)velocityZ) / 10) * timeElapsed)) / 100);
				}
				else
					return new Vector3(((float)positionX) / 100, ((float)positionY) / 100, ((float)positionZ) / 100);
			}

			//Converting to displayable coordinates
			public String letterCoord()
			{
				return Helpers.posToLetterCoord(positionX, positionY);
			}
		}

		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Notifies a bunch of players of item drops
		/// </summary>
		static public void Object_Items(IEnumerable<Player> players, IEnumerable<Arena.ItemDrop> drop)
		{	//Create the notification packet
			SC_Items items = new SC_Items();

			items.items = drop;

			//Route the packet
			foreach (Player p in players)
				p._client.sendReliable(items);
		}

		/// <summary>
		/// Notifies a player of item drops
		/// </summary>
		static public void Object_Items(Player p, IEnumerable<Arena.ItemDrop> drop)
		{	//Create the notification packet
			SC_Items items = new SC_Items();

			items.items = drop;

			//Route the packet
			p._client.sendReliable(items);
		}

		/// <summary>
		/// Updates the status of an item drop in the specified clients
		/// </summary>
		static public void Object_ItemDropUpdate(IEnumerable<Player> players, ushort dropID, ushort quantity)
		{	//Create the notification packet
			SC_ItemDrop destroy = new SC_ItemDrop();
			destroy.dropID = dropID;
			destroy.quantity = quantity;

			//Route the packet
			foreach (Player p in players)
				p._client.sendReliable(destroy);
		}

		/// <summary>
		/// Notifies a bunch of players of a new item drop
		/// </summary>
		static public void Object_ItemDrops(IEnumerable<Player> players, IEnumerable<Arena.ItemDrop> drop)
		{
			foreach (Arena.ItemDrop item in drop)
			{	//Create the notification packet
				SC_ItemDrop destroy = new SC_ItemDrop();
				destroy.dropID = item.id;
				destroy.quantity = (ushort)item.quantity;

				//Route the packet
				foreach (Player p in players)
					p._client.sendReliable(destroy);
			}
		}

		/// <summary>
		/// Notifies a bunch of players of a new item drop
		/// </summary>
		static public void Object_ItemDrop(IEnumerable<Player> players, Arena.ItemDrop drop)
		{	//Create the notification packet
			SC_Items items = new SC_Items();

			items.singleItem = drop;

			//Route the packet
			foreach (Player p in players)
				p._client.sendReliable(items);
		}

		/// <summary>
		/// Instructs the client to reset and disable all flags
		/// </summary>
		static public void Object_FlagsReset(IEnumerable<Player> p)
		{	//Prepare the packet
			SC_Flags flags = new SC_Flags();
			flags.bDisable = true;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(flags);
		}

		/// <summary>
		/// Sends a flag update to the specified players
		/// </summary>
		static public void Object_Flags(IEnumerable<Player> p, Arena.FlagState update)
		{	//Prepare the packet
			SC_Flags flags = new SC_Flags();
			flags.singleUpdate = update;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(flags);
		}

		/// <summary>
		/// Sends a series of flag updates to the specified player
		/// </summary>
		static public void Object_Flags(Player p, IEnumerable<Arena.FlagState> update)
		{	//Prepare the packet
			SC_Flags flags = new SC_Flags();
			flags.flags = update;

			//Send to each recipient
			p._client.sendReliable(flags);
		}

		/// <summary>
		/// Sends a series of flag updates to the specified players
		/// </summary>
		static public void Object_Flags(IEnumerable<Player> p, IEnumerable<Arena.FlagState> update)
		{	//Prepare the packet
			SC_Flags flags = new SC_Flags();
			flags.flags = update;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(flags);
		}

		/// <summary>
		/// Sends a lio update to the specified players
		/// </summary>
		static public void Object_LIOs(IEnumerable<Player> p, Arena.SwitchState update)
		{	//Prepare the packet
			SC_LIOUpdates lios = new SC_LIOUpdates();
			lios.singleUpdate = update;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(lios);
		}

		/// <summary>
		/// Sends a series of lio updates to the specified player
		/// </summary>
		static public void Object_LIOs(Player p, IEnumerable<Arena.SwitchState> update)
		{	//Prepare the packet
			SC_LIOUpdates lios = new SC_LIOUpdates();
			lios.objects = update;

			//Send to each recipient
			p._client.sendReliable(lios);
		}

		/// <summary>
		/// Sends a series of lio updates to the specified players
		/// </summary>
		static public void Object_LIOs(IEnumerable<Player> p, IEnumerable<Arena.SwitchState> update)
		{	//Prepare the packet
			SC_LIOUpdates lios = new SC_LIOUpdates();
			lios.objects = update;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(lios);
		}

		/// <summary>
		/// Indicates the destruction of a vehicle
		/// </summary>
		static public void Object_VehicleDestroy(IEnumerable<Player> p, Vehicle vehicle)
		{	//Prepare the packet
			SC_VehicleDestroy des = new SC_VehicleDestroy();

			des.vehicleID = vehicle._id;

			//Send to recipient
			foreach (Player player in p)
				player._client.sendReliable(des);
		}

		/// <summary>
		/// Binds new information to a player's base vehicle
		/// </summary>
		static public void Object_VehicleBind(IEnumerable<Player> p, Vehicle vehicle1, short vehicleID)
		{	//Prepare the packet
			SC_BindVehicle bind = new SC_BindVehicle();

			bind.Vehicle1 = vehicle1;
			bind.vehicleID = vehicleID;

			//Send to recipient
			foreach (Player player in p)
				player._client.sendReliable(bind);
		}

		/// <summary>
		/// Binds new information to an existing vehicle
		/// </summary>
		static public void Object_VehicleBind(IEnumerable<Player> p, Vehicle vehicle)
		{	//Prepare the packet
			SC_BindVehicle bind = new SC_BindVehicle();

			bind.Vehicle1 = vehicle;
			bind.Vehicle2 = ((vehicle._inhabitant == null) ? null : vehicle._inhabitant._baseVehicle);

			//Send to recipient
			foreach (Player player in p)
				player._client.sendReliable(bind);
		}

		/// <summary>
		/// Binds new information to an existing vehicle
		/// </summary>
		static public void Object_VehicleBind(IEnumerable<Player> p, Vehicle vehicle1, Vehicle vehicle2)
		{	//Prepare the packet
			SC_BindVehicle bind = new SC_BindVehicle();

			bind.Vehicle1 = vehicle1;
			bind.Vehicle2 = vehicle2;

			//Send to recipient
			foreach (Player player in p)
				player._client.sendReliable(bind);
		}

		/// <summary>
		/// Binds new information to an existing vehicle
		/// </summary>
		static public void Object_VehicleBind(IEnumerable<Player> p, 
			Vehicle vehicle1, Vehicle vehicle2, Action completionCallback)
		{	//Prepare the packet
			SC_BindVehicle bind = new SC_BindVehicle();

			bind.Vehicle1 = vehicle1;
			bind.Vehicle2 = vehicle2;

			//Send to recipient
			foreach (Player player in p)
			{	//If this is the player's new vehicle, watch for completion
				if (vehicle1._inhabitant == player)
					player._client.sendReliable(bind, completionCallback);
				else
					player._client.sendReliable(bind);
			}
		}

		/// <summary>
		/// Sends a series of vehicle updates to the specified player
		/// </summary>
		static public void Object_Vehicles(Player p, IEnumerable<Vehicle> update)
		{	//Prepare the packet
			SC_Vehicles vehicles = new SC_Vehicles();
			vehicles.vehicles = update;

			//Send to recipient
			p._client.sendReliable(vehicles);
		}

		/// <summary>
		/// Sends a vehicle update to the specified players
		/// </summary>
		static public void Object_Vehicles(IEnumerable<Player> p, Vehicle update)
		{	//Prepare the packet
			SC_Vehicles vehicles = new SC_Vehicles();
			vehicles.singleUpdate = update;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(vehicles);
		}

		/// <summary>
		/// Sends a series of vehicle updates to the specified players
		/// </summary>
		static public void Object_Vehicles(IEnumerable<Player> p, IEnumerable<Vehicle> update)
		{	//Prepare the packet
			SC_Vehicles vehicles = new SC_Vehicles();
			vehicles.vehicles = update;

			//Send to each recipient
			foreach (Player player in p)
				player._client.sendReliable(vehicles);
		}

		/// <summary>
		/// Sends a player update to the specified player
		/// </summary>
		static public void Object_Players(Player p, Player update)
		{	//Sanity checks
			if (update._baseVehicle == null)
			{
				Log.write(TLog.Warning, "Unable to update a player '{0}' without a baseVehicle set!", update._alias);
				return;
			}

			//Prepare the packet
			SC_PlayerEnter players = new SC_PlayerEnter();
			players.singlePlayer = update;

			p._client.sendReliable(players);
		}

		/// <summary>
		/// Sends a series of player updates to the specified player
		/// </summary>
		static public void Object_Players(IEnumerable<Player> p, Player update)
		{	//Sanity checks
			if (update._baseVehicle == null)
			{
				Log.write(TLog.Warning, "Unable to update a player '{0}' without a baseVehicle set!", update._alias);
				return;
			}

			//Prepare the packet
			SC_PlayerEnter players = new SC_PlayerEnter();
			players.singlePlayer = update;

			//Send to each recipient
			foreach (Player player in p)
				if (player != update)
					player._client.sendReliable(players);	
		}

		/// <summary>
		/// Sends a series of player updates to the specified player
		/// </summary>
		static public void Object_Players(Player p, IEnumerable<Player> updates)
		{	//Prepare the packet
			SC_PlayerEnter players = new SC_PlayerEnter();

			players.exclude = p;
			players.players = updates;

			//Done!
			if (updates.Any(plyr => plyr != null))
				p._client.sendReliable(players);
		}

		/// <summary>
		/// Notifies the alighting of a certain player
		/// </summary>
		static public void Object_PlayerLeave(Player p, Player leaving)
		{	//Prepare the packet
			SC_PlayerLeave update = new SC_PlayerLeave();
			update.playerID = leaving._id;

			p._client.sendReliable(update);
		}

		/// <summary>
		/// Notifies the alighting of a certain player
		/// </summary>
		static public void Object_PlayerLeave(IEnumerable<Player> p, Player leaving)
		{	//Prepare the packet
			SC_PlayerLeave update = new SC_PlayerLeave();
			update.playerID = leaving._id;

			foreach (Player player in p)
				player._client.sendReliable(update);
		}
	}
}
