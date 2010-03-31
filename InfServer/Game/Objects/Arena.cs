using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////
		public ZoneServer _server;						//The server we belong to
		private List<Player> _players;					//The list of players in this arena
		private Dictionary<string, Team> _teams;		//The list of teams, indexed by name

		private Dictionary<ushort, Vehicle> _vehicles;	//The vehicles belonging to the arena, indexed by id
		private ushort _lastVehicleKey;					//The last vehicle key which was allocated

		private Dictionary<ushort, ItemDrop> _items;	//The items belonging to the arena, indexed by id
		private ushort _lastItemKey;					//The last item key which was allocated

		public string _name;							//The name of this arena

		//Events
		public event Action<Arena> Close;				//Called when an arena runs out of players and is closed

		//Settings
		static public int maxVehicles;					//The maximum amount of vehicles we can have active
		static public int maxItems;						//The maximum amount of items we can have laying about


		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		/// <summary>
		/// Returns a list of the active players in the arena
		/// </summary>
		public List<Player> Players
		{
			get
			{
				return _players;
			}
		}

		/// <summary>
		/// Is this arena invisible to normal players?
		/// </summary>
		public bool bPrivate
		{
			get
			{
				return _name[0] == '#';
			}
		}

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Represents a dropped item
		/// </summary>
		public class ItemDrop
		{
			public ushort id;			//The ID of the item pile
			public ItemInfo item;		//The type of type
			
			public short quantity;		//The amount in the pile
			public short positionX;		//The location of the pile
			public short positionY;		//
		}
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Arena(ZoneServer server)
		{
			_server = server;

			_players = new List<Player>();
			_teams = new Dictionary<string, Team>();

			_vehicles = new Dictionary<ushort, Vehicle>();
			_lastVehicleKey = (ushort)ZoneServer.maxPlayers;	//The first vehicle key will naturally start
																//at the edge of the player base vehicles.
			_items = new Dictionary<ushort, ItemDrop>();
			_lastItemKey = 0;
		}

		#region State
		/// <summary>
		/// Initializes arena details
		/// </summary>
		public void init()
		{	//Create all our teams, as per the zone config
			ushort idx = 0;

			foreach (Assets.ConfigInfo.TeamInfo ti in _server._zoneConfig.teams)
			{	//Populate the new class
				Team newTeam = new Team(this, _server);

				newTeam._name = ti.name;
				newTeam._id = idx++;

				newTeam._info = ti;

				_teams.Add(ti.name, newTeam);
			}
		}

		/// <summary>
		/// Cleans up the arena and removes it from the zone server list
		/// </summary>
		public void close()
		{	//Call our close event
			if (Close != null)
				Close(this);
		}

		/// <summary>
		/// Called when a new player is entering our arena
		/// </summary>
		public void newPlayer(Player player)
		{	//Fake a player state
			player._cash = 13337;
			player._experience = 73331;
			player._experienceTotal = 80000;

			for (int i = 0; i < 8; ++i)
			{
				Player.SkillItem s = new Player.SkillItem();

				s.skill = new Assets.SkillInfo();
				s.skill.skillID = i + 1;
				s.quantity = 0;
				player._skills.Add(s);
			}

			int[] modWeps = new int[] {	1026, 1042, 1078, 1079, 1080, 1098, 1099, 1116, 1119, 1121, 1123, 1124, 427, 1125,
										1126, 1127, 1128, 1129, 1130, 1158, 1132, 1133, 1134, 1135, 1136, 1137, 1156, 1139,
										1140, 1141, 1142, 1143, 1144, 1148, 1149, 1150, 1151, 1152, 1153, 1154, 1155,
										3049, 3006, 3046, 3047, 3007, 3048, 3012, 3015, 3039, 3040, 3041, 3042, 3043, 3044};

			foreach (int wid in modWeps)
			{
				Player.InventoryItem i = new Player.InventoryItem();
				i.item = _server._assets.getItemByID(wid);
				i.quantity = 1;
				player._inventory.Add(i);
			}

			//We're entering the arena..
			player._bIngame = false;

			//Make sure he's receiving ingame packets
			player._client.sendReliable(new SC_SetIngame());

			//Prepare his base vehicle (the vehicle his class naturally inherits
			Vehicle baseVehicle = new Vehicle(12);
			player._baseVehicle = baseVehicle;

			//HACK: Just three teams for now
			_teams.Values.SingleOrDefault(team => team._id == player._id % 3).addPlayer(player);

			//Define the player's self object
			Helpers.Object_Players(player, player);

			//Initialize the player's state
			Helpers.Player_StateInit(player);

			//Load the arena's item state
			Helpers.Object_Items(player, _items.Values);

			//Make sure the player is aware of every player in the arena
			Helpers.Object_Players(player, _players,
				delegate()
				{	//And make sure everyone is aware of him
					Helpers.Object_Players(_players, player);

					//We can now add him to our list of players
					_players.Add(player);
					player._arena = this;

					//Consider him loaded!
					player.setIngame();
					player.warp(SC_PlayerWarp.WarpMode.Normal, 500, 4500);
				}
			);

			/*SC_ChangeTeam tchange = new SC_ChangeTeam();
			tchange.playerID = player._id;
			tchange.unk2 = 0x7D00;
			tchange.teamname = "spec" + new Random().Next();
			player._client.send(tchange);*/

			/*SC_Vehicles vehicles = new SC_Vehicles();
			SC_Vehicles.VehicleInfo vi = new SC_Vehicles.VehicleInfo();
			vi.vehicleID = 123;
			vi.vehicleTypeID = 400;
			vi.unk1 = -1;
			vi.extraData = new byte[] { 0x88, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x56, 0x56, 0x00, 0x00 };
			vehicles.vehicles.Add(vi);
			vi = new SC_Vehicles.VehicleInfo();
			vi.vehicleID = 0x1B;
			vi.vehicleTypeID = 1;
			vi.unk1 = -1;
			vi.extraData = new byte[] { 0xEA, 0x19, 0xAF, 0x25 };
			vehicles.vehicles.Add(vi);

			client.send(vehicles);

			SC_Items items = new SC_Items();
			Random rand = new Random();

			for (int i = 0; i < 35; ++i)
			{
				SC_Items.ItemInfo ii = new SC_Items.ItemInfo();
				ii.pos_x = (ushort)rand.Next(20, 400);
				ii.pos_y = (ushort)rand.Next(20, 400);
				ii.itemID = (ushort)(i + 1);
				ii.itemTypeID = 2005;
				items.items.Add(ii);
			}

			client.send(items);

			SC_ChangeTeam tchange = new SC_ChangeTeam();
			tchange.playerID = 0x1B;
			tchange.unk2 = 0x7D00;
			tchange.teamname = "spec";
			client.send(tchange);

			//Let's create a spectator vehicle
			SC_Vehicles.VehicleInfo vi = new SC_Vehicles.VehicleInfo();
			vi.vehicleID = 0x2B;
			vi.vehicleTypeID = 1;
			vi.unk1 = -1;
			vi.extraData = new byte[] { 0, 0, 0, 0 };

			SC_Vehicles specVeh = new SC_Vehicles(vi);
			client.send(specVeh);

			//Do some binds
			SC_BindVehicle bind = new SC_BindVehicle();
			bind.Vehicle1 = 0x1B;
			bind.Vehicle2 = -1;
			bind.extraData = new byte[] { 0x33, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			//client.send(bind);
			bind = new SC_BindVehicle();
			bind.Vehicle1 = 0x2B;
			bind.Vehicle2 = 0x1B;
			bind.extraData = new byte[] { 0x08, 0xDF, 0x15, 0xD9  };
			client.send(bind);

			SC_EnterArena test = new SC_EnterArena();
			client.send(test);*/
		}

		/// <summary>
		/// Handles the loss of a player
		/// </summary>
		public void lostPlayer(Player player)
		{	//Sob, let him go
			_players.Remove(player);

			//Do we have any players left?
			if (_players.Count == 0)
				//Nope. It's closing time.
				close();
			else
				//Notify everyone else of his departure
				Helpers.Object_PlayerLeave(_players, player);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Vehicle newVehicle(ushort type)
		{	//Too many vehicles?
			if (_vehicles.Count == maxVehicles)
			{
				Log.write(TLog.Warning, "Vehicle list full.");
				return null;
			}

			//We want to continue wrapping around the vehicleid limits
			//looking for empty spots.
			ushort vk;

			for (vk = _lastVehicleKey; vk <= UInt16.MaxValue; ++vk)
			{	//If we've reached the maximum, wrap around
				if (vk == UInt16.MaxValue)
				{
					vk = (ushort)ZoneServer.maxPlayers;
					continue;
				}

				//Does such a vehicle exist?
				if (_vehicles.ContainsKey(vk))
					continue;

				//We have a space!
				break;
			}

			_lastVehicleKey = vk;

			//Create our vehicle class		
			Vehicle newVehicle = new Vehicle(type);

			newVehicle._arena = this;
			newVehicle._id = vk;

			_vehicles[vk] = newVehicle;
			return newVehicle;
		}

		/// <summary>
		/// Creates an item drop at the specified location
		/// </summary>
		public ItemDrop itemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY)
		{	//Too many items?
			if (_items.Count == maxItems)
			{
				Log.write(TLog.Warning, "Item count full.");
				return null;
			}

			//We want to continue wrapping around the vehicleid limits
			//looking for empty spots.
			ushort ik;

			for (ik = _lastItemKey; ik <= Int16.MaxValue; ++ik)
			{	//If we've reached the maximum, wrap around
				if (ik == Int16.MaxValue)
				{
					ik = (ushort)ZoneServer.maxPlayers;
					continue;
				}

				//Does such an item exist?
				if (_items.ContainsKey(ik))
					continue;

				//We have a space!
				break;
			}

			_lastItemKey = ik;

			//Create our drop class		
			ItemDrop id = new ItemDrop();

			id.item = item;
			id.id = ik;
			id.quantity = (short)quantity;
			id.positionX = positionX;
			id.positionY = positionY;

			//Add it to our list
			_items[ik] = id;
			
			//Notify the arena
			Helpers.Object_ItemDrop(_players, id);
			return id;
		}
		#endregion

		#region Update
		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		public void handlePlayerPickup(Player from, CS_PlayerPickup update)
		{	//Find the itemdrop in question
			ItemDrop drop;

			if (!_items.TryGetValue(update.itemID, out drop))
				//Doesn't exist
				return;

			//Sanity checks
			if (update.quantity > drop.quantity)
				return;
			else if (update.quantity == drop.quantity)
				//Delete the drop
				_items.Remove(drop.id);
			else
				drop.quantity = (short)(drop.quantity - update.quantity);

			//Add the pickup to inventory!
			from.inventoryModify(drop.item, update.quantity);

			//Remove the item from player's clients
			Helpers.Object_ItemDropUpdate(_players, update.itemID, 0);
		}

		/// <summary>
		/// Triggered when a player requests to drop an item
		/// </summary>
		public void handlePlayerDrop(Player from, CS_PlayerDrop update)
		{	//Get the item into
			ItemInfo item = _server._assets.getItemByID(update.itemID);
			if (item == null)
			{
				Log.write(TLog.Warning, "Player requested to drop invalid item. {0}", from);
				return;
			}
			
			//Perform some sanity checks
			if (!item.droppable)
				return;
			if (!Helpers.isInRange(100,
				from._state.positionX, from._state.positionY,
				update.positionX, update.positionY))
				return;

			//Update his inventory
			if (from.inventoryModify(item, -update.quantity))
				//Create an item spawn
				itemSpawn(item, update.quantity, update.positionX, update.positionY);
		}

		/// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
		public void handlePlayerExplosion(Player from, CS_Explosion update)
		{	//Warpie doo
			//from.warp(SC_PlayerWarp.WarpMode.Normal, update.positionX, update.positionY);
		}

		/// <summary>
		/// Triggered when a player has sent an update packet
		/// </summary>
		public void handlePlayerUpdate(Player from, CS_PlayerUpdate update)
		{	//Update the player's state
			from._state.health = update.health;
			from._state.energy = update.energy;

			from._state.velocityX = update.velocityX;
			from._state.velocityY = update.velocityY;
			from._state.velocityZ = update.velocityZ;

			from._state.positionX = update.positionX;
			from._state.positionY = update.positionY;
			from._state.positionZ = update.positionZ;

			from._state.yaw = update.yaw;
			from._state.direction = update.direction;
			from._state.unk1 = update.unk1;

			//Route it to all players!
			Helpers.Player_RouteUpdate(_players, from, update);
		}

		/// <summary>
		/// Triggered when a player has sent a death packet
		/// </summary>
		public void handlePlayerDeath(Player from, CS_PlayerDeath update)
		{	//Sanity checks
			Player killer = _players.SingleOrDefault(plyr => plyr._id == update.killerPlayerID);

			if (killer == null)
			{
				Log.write(TLog.Warning, "Player {0} gave invalid killer ID.", from);
				return;
			}
			
			//Route the death packet
			Helpers.Player_RouteKill(_players, update, from, 1337, 1337, 1337, 1337);

			//Respawn him!
			from.warp(SC_PlayerWarp.WarpMode.Respawn, 500, 4500);
		}
		#endregion

		#region Social
        /// <summary>        
        /// Returns the list of player targets for public chat
        /// </summary>        
        public IEnumerable<Player> getChatTargets()
        {
            return _players;
        }

		/// <summary>
		/// Triggered when a player has sent chat to the entire arena
		/// </summary>
		public void playerArenaChat(Player from, CS_Chat chat)
		{	//Route it to our entire player list!
			Helpers.Player_RouteChat(this, from, chat);
		}

		/// <summary>
		/// Sends an arena message to the entire arena
		/// </summary>
		public void sendArenaMessage(string message)
		{	//Relay the message
			Helpers.Social_ArenaChat(this, message);
		}
		#endregion
	}
}
