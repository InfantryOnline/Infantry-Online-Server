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
	public partial class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////
		protected Dictionary<string, Team> _teams;				//The list of teams, indexed by name
		protected SortedDictionary<int, Team> _freqTeams;		//The list of teams, indexed by frequency

		protected ObjTracker<Vehicle> _vehicles;				//The vehicles belonging to the arena, indexed by id
		private ushort _lastVehicleKey;							//The last vehicle key which was allocated

		protected SortedDictionary<ushort, ItemDrop> _items;	//The items belonging to the arena, indexed by id
		private ushort _lastItemKey;							//The last item key which was allocated


		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		/// <summary>
		/// Returns a list of public teams
		/// </summary>
		public IEnumerable<Team> PublicTeams
		{
			get
			{
				return _teams.Values.Where(team => team.IsPublic);
			}
		}

		/// <summary>
		/// Returns a list of teams present in the arena
		/// </summary>
		public IEnumerable<Team> Teams
		{
			get
			{
				return _teams.Values;
			}
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		#region State
		/// <summary>
		/// Initializes arena details
		/// </summary>
		private void initState()
		{	//First a spectator team
			Team newTeam = new Team(this, _server);

			newTeam._isPrivate = true;
			newTeam._name = "spec";
			newTeam._id = 1000;
			_teams.Add("spec", newTeam);

			//Create all our teams, as per the zone config
			int id = 0;

			foreach (CfgInfo.TeamInfo ti in _server._zoneConfig.teams)
			{	//Populate the new class
				newTeam = new Team(this, _server);

				newTeam._name = ti.name;
				newTeam._id = (short)id;

				newTeam._info = ti;

				_teams.Add(ti.name.ToLower(), newTeam);
				_freqTeams.Add(id++, newTeam);
			}
		}

		/// <summary>
		/// Called when a new player is entering our arena
		/// </summary>
		public void newPlayer(Player player)
		{	///////////////////////////////////////////////
			// Prepare the player state
			///////////////////////////////////////////////
			//We're entering the arena..
			player._arena = this;
			player.migrateStats();

			//TODO: Check rules for whether player enters in spec
			player._bSpectator = true;
			player._team = _teams["spec"];

			//TEMP: In this arena, he's king
			player._permissionTemp = InfServer.Data.PlayerPermission.Sysop;

			//Find his natural vehicle id and prepare the class
			Player.SkillItem baseSkill = player._skills.Values.FirstOrDefault(skill => skill.skill.DefaultVehicleId != -1);
			int baseVehicleID = (baseSkill == null) ? _server._zoneConfig.publicProfile.defaultVItemId : baseSkill.skill.DefaultVehicleId;
			Vehicle baseVehicle = new Vehicle(_server._assets.getVehicleByID(baseVehicleID), this);

			baseVehicle._bBaseVehicle = true;
			baseVehicle._id = player._id;
			baseVehicle._state = player._state;		//Player and basevehicle share same state

			player._baseVehicle = baseVehicle;


			///////////////////////////////////////////////
			// Send the player state
			///////////////////////////////////////////////
			//Make sure he's receiving ingame packets
			player._client.sendReliable(new SC_SetIngame());

			//Add him to our list of players. We want to do this now so he doesn't lose 
			//info about anything happening until then.
			_players.Add(player);

			//Define the player's self object
			Helpers.Object_Players(player, player);

			//Make sure the player is aware of every player in the arena
		    Helpers.Object_Players(player, Players);

			//Load the arena's item state  
			Helpers.Object_Items(player, _items.Values);         

			//Load the arena's various lio objects
			Helpers.Object_Flags(player, _flags.Values);
			Helpers.Object_LIOs(player, _switches.Values);

			//Load the vehicles in the arena
			if (_vehicles.Count > 0)
				Helpers.Object_Vehicles(player, _vehicles);

			//Initialize the player's state
			Helpers.Player_StateInit(player,
				delegate()
				{	//And make sure everyone is aware of him
					Helpers.Object_Players(Players, player);

					//Consider him loaded!
					player.spec("spec");
					player.setIngame();

					//Load the tickers
					Helpers.Arena_Message(player, _tickers.Values);
				}
			);
		}

		/// <summary>
		/// Resets all game-specific vehicles in the arena
		/// </summary>
		public void resetVehicles()
		{	//Kill each vehicle which isn't a spectator
			List<Vehicle> vehicles = new List<Vehicle>(_vehicles);

			foreach (Vehicle veh in vehicles)
				if (veh._type.Type != VehInfo.Types.Spectator)
					veh.destroy(true, true);
		}

		/// <summary>
		/// Resets all items in the arena
		/// </summary>
		public void resetItems()
		{	//Kill each item drop
			foreach (ItemDrop itm in _items.Values)
				itm.quantity = 0;

			Helpers.Object_Items(Players, _items.Values);
		}

		/// <summary>
		/// Handles the loss of a player
		/// </summary>
		public void lostPlayer(Player player)
		{	//Sob, let him go
			_players.Remove(player);
			_playersIngame.Remove(player);

			player.onLeaveArena();
			
			//Do we have any players left?
			if (TotalPlayerCount == 0)
				//Nope. It's closing time.
				close();
			else
				//Notify everyone else of his departure
				Helpers.Object_PlayerLeave(Players, player);
		}

		/// <summary>
		/// Handles the loss of a vehicle
		/// </summary>
		public void lostVehicle(Vehicle vehicle, bool bRemove)
		{	//Sob, let it go
			if (bRemove)
				_vehicles.Remove(vehicle);

			//Notify everyone else of it's destruction
			Helpers.Object_VehicleDestroy(Players, vehicle);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Vehicle newVehicle(ushort type)
		{	//Redirect
			return newVehicle(_server._assets.getVehicleByID(type), null, null, null, null);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Vehicle newVehicle(VehInfo type, Team team, Player creator)
		{	//Redirect
			return newVehicle(type, team, creator, null, null);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Vehicle newVehicle(VehInfo type, Team team, Player creator, Helpers.ObjectState state)
		{	//Redirect
			return newVehicle(type, team, creator, state, null);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Vehicle newVehicle(VehInfo type, Team team, Player creator, Helpers.ObjectState state, Action<Vehicle> setupCB)
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
					vk = 5001;
					continue;
				}

				//Does such a vehicle exist?
				if (_vehicles.getObjByID(vk) != null)
					continue;

				//We have a space!
				break;
			}

			//TODO: There might be some kind of strange bug regarding re-used vehicle
			//		ids, even if you attempt to dispose of them.
			_lastVehicleKey = (ushort)(vk + 1);

			//Create our vehicle class		
			Vehicle veh;
			
			if (type.Type == VehInfo.Types.Computer)
				veh = new Computer(type as VehInfo.Computer, this);
			else
				veh = new Vehicle(type, this);

			veh._id = vk;

			veh._team = team;
			veh._creator = creator;

			veh._tickUnoccupied = veh._tickCreation = Environment.TickCount;

			if (state != null)
			{
				veh._state.positionX = state.positionX;
				veh._state.positionY = state.positionY;
				veh._state.positionZ = state.positionZ;
				veh._state.yaw = state.yaw;
			}

			veh.assignDefaultState();

			//Custom setup?
			if (setupCB != null)
				setupCB(veh);

			//This uses the new ID automatically
			_vehicles.Add(veh);

			//Notify everyone of the new vehicle
			Helpers.Object_Vehicles(Players, veh);

			//Handle dependent vehicles?
			int slot = 0;

			foreach (int vid in veh._type.ChildVehicles)
			{	//Nothing?
				slot++;

				if (vid <= 0)
					continue;

				//Find the vehicle type
				VehInfo childType = _server._assets.getVehicleByID(vid);

				if (childType == null)
				{
					Log.write(TLog.Error, "Invalid child vehicle id '{0}' for {1}.", vid, type);
					continue;
				}

				//Create it!
				Vehicle child = newVehicle(childType, team, creator, state,
					delegate(Vehicle c)
					{
						c._parent = veh;
						c._parentSlot = slot - 1;
					}
				);

				veh._childs.Add(child);
			}

			//If it's not a spectator or dependent vehicle, let the arena pass it to the script
			if (type.Type != VehInfo.Types.Dependent && type.Type != VehInfo.Types.Spectator)
				handleVehicleCreation(veh, team, creator);

			return veh;
		}

		/// <summary>
		/// Creates an item drop at the specified location
		/// </summary>
		public ItemDrop itemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY)
		{			
			//Too many items?
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
			Helpers.Object_ItemDrop(Players, id);
			return id;
		}
		#endregion
	}
}
