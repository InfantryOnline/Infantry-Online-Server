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
	// Vehicle Class
	/// Represents a single vehicle in an arena
	///////////////////////////////////////////////////////
	public class Vehicle : ILocatable
	{	// Member variables
		///////////////////////////////////////////////////
		public Arena _arena;				//The arena we belong to
		public VehInfo _type;				//The type of vehicle we represent

		public Team _team;					//The team we belong to
		public Player _creator;				//The player which created us

		public List<Vehicle> _childs;		//Our child vehicles
		public Vehicle _parent;				//Our parent vehicle, if we're a dependent vehicle
		public int _parentSlot;				//The slot we occupy in the parent vehicle

		public bool _bBotVehicle;			//Are we a bot-owned vehicle?
		public bool _bBaseVehicle;			//Are we a base vehicle, implied by a player?
		public Player _inhabitant;			//The player, if any, that's inside the vehicle

		public ushort _id;					//Our vehicle ID

		#region Game state
		public Helpers.ObjectState _state;	//The state of our vehicle!

		//Game timers
		public int _tickCreation;			//The time at which the vehicle was created
		public int _tickUnoccupied;			//The time at which the vehicle was last unoccupied
		public int _tickDead;				//The time at which the vehicle was last dead
		#endregion


		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		/// <summary>
		/// Is this player currently dead?
		/// </summary>
		public bool IsDead
		{
			get
			{
				return _state.health == 0;
			}
		}

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes

		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Vehicle(VehInfo type, Arena arena)
		{	//Populate variables
			_type = type;
			_arena = arena;

			_state = new Helpers.ObjectState();
			_childs = new List<Vehicle>();
		}

		/// <summary>
		/// Generic constructor
		/// </summary>
		public Vehicle(VehInfo type, Helpers.ObjectState state, Arena arena)
		{	//Populate variables
			_type = type;
			_arena = arena;

			_state = state;
			_childs = new List<Vehicle>();
		}

		/// <summary>
		/// Initialize the state with the default health, energy, etc
		/// </summary>
		public void assignDefaultState()
		{
			_state.health = (short)(_arena._server._zoneConfig.soul.energyShieldMode == 2 ? 1 : _type.Hitpoints);
			_state.energy = (short)_type.EnergyMax;
		}

		/// <summary>
		/// Updates the state for any child vehicles if necessary
		/// </summary>
		public void propagateState()
		{
			foreach (Vehicle child in _childs)
			{	//If it's occupied, this isn't necessary
				if (child._inhabitant != null)
					continue;

				child._state.positionX = _state.positionX;
				child._state.positionY = _state.positionY;
				child._state.positionZ = _state.positionZ;

				child._state.velocityX = _state.velocityX;
				child._state.velocityY = _state.velocityY;
				child._state.velocityZ = _state.velocityZ;
			}
		}

		#region ILocatable functions
		public ushort getID() { return _id; }
		public Helpers.ObjectState getState() { return _state; }
		#endregion

		#region State
		/// <summary>
		/// Causes the vehicle to die
		/// </summary>
		public void kill(Player killer)
		{	//Set our health to 0
			_state.health = 0;

			_tickDead = Environment.TickCount;

			//Notify the arena
			_arena.handleVehicleDeath(this, killer, _inhabitant);
		}

		/// <summary>
		/// The vehicle is being destroyed, clean up assets
		/// </summary>
		public void destroy(bool bRestoreBase)
		{	//Redirect
			destroy(bRestoreBase, true);
		}

		/// <summary>
		/// The vehicle is being destroyed, clean up assets
		/// </summary>
		public virtual void destroy(bool bRestoreBase, bool bRemove)
		{	//If we have a player, kick him out
			playerLeave(bRestoreBase);

			//Notify the arena of our destruction
			_arena.lostVehicle(this, bRemove);
		}
		#endregion

		#region Game State
		/// <summary>
		/// Called when a player is entering the vehicle
		/// </summary>
		public bool playerEnter(Player player)
		{	//We want to ignore new updates until we
			//have confirmation that the player has changed vehicle
			player._bIgnoreUpdates = true;	
			
			//Free up the old vehicle
			if (player._occupiedVehicle != null)
				player._occupiedVehicle.playerLeave(false);

			//Set the inhabitants
			_inhabitant = player;
			player._baseVehicle._inhabitant = null;

			//We're now occupied
			player._occupiedVehicle = this;
			_tickUnoccupied = 0;

			//Disable our base vehicle and enable the new
			Helpers.Object_VehicleBind(_arena.Players, player._baseVehicle, null);
			Helpers.Object_VehicleBind(_arena.Players, this, player._baseVehicle,
				delegate() 
				{
					player._bIgnoreUpdates = false;	
				}
			);
			return true;
		}

		/// <summary>
		/// Called when a player is exiting the vehicle
		/// </summary>
		public void playerLeave(bool bRestoreBase)
		{	//Do we even have an inhabitant?
			if (_inhabitant == null)
				return;

			if (bRestoreBase)
			{	//We want to ignore new updates until we
				//have confirmation that the player has changed vehicle
				Player exInhabitant = _inhabitant;
				exInhabitant._bIgnoreUpdates = true;	

				_inhabitant._baseVehicle._inhabitant = _inhabitant;
				Helpers.Object_VehicleBind(_arena.Players, _inhabitant._baseVehicle, _inhabitant._baseVehicle,
					delegate() 
					{ 
						exInhabitant._bIgnoreUpdates = false; 
					});
			}

			//We're no longer inhabited
			_tickUnoccupied = Environment.TickCount;
			_inhabitant._occupiedVehicle = null;
			_inhabitant = null;

			Helpers.Object_VehicleBind(_arena.Players, this, null);
		}
		#endregion

		#region Helpers
		/// <summary>
		/// Updates the vehicle state with the arena
		/// </summary>
		public void update(bool bSelfOccupy)
		{	//Do it!
			if (!bSelfOccupy)
				Helpers.Object_VehicleBind(_arena.Players, this);
			else
				Helpers.Object_VehicleBind(_arena.Players, this, this);
		}
		#endregion
	}
}
