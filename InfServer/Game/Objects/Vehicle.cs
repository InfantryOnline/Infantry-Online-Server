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
using Bnoerj.AI.Steering;

namespace InfServer.Game
{
	// Vehicle Class
	/// Represents a single vehicle in an arena
	///////////////////////////////////////////////////////
	public class Vehicle : CustomObject, ILocatable
	{	// Member variables
		///////////////////////////////////////////////////
		public bool bCondemned;				//Is the vehicle ready to be deleted?
		public Arena _arena;				//The arena we belong to
        public VehInfo _type;				//The type of vehicle we represent

		private VehicleAbstract _abstract;	//Used for communicating with the opensteer framework

		public Team _team;					//The team we belong to
		public Player _creator;				//The player which created us
        public Team _owner;                 //The current team which owns us

		public List<Vehicle> _childs;		//Our child vehicles
		public Vehicle _parent;				//Our parent vehicle, if we're a dependent vehicle
		public int _parentSlot;				//The slot we occupy in the parent vehicle

		public bool _bBotVehicle;			//Are we a bot-owned vehicle?
		public bool _bBaseVehicle;			//Are we a base vehicle, implied by a player?
		public Player _inhabitant;			//The player, if any, that's inside the vehicle
        public int _lane;

		public ushort _id;					//Our vehicle ID

        private int _relativeID;            //Relative ID of the vehicle, can be overridden
        public int relativeID               //when the vehicle spawns
        {
            get { return (_relativeID == 0 ? _type.RelativeId : _relativeID); }
            set { _relativeID = value; }
        }

		#region Game state
		public Helpers.ObjectState _state;	//The state of our vehicle!
		public List<Player> _attackers;		//The list of players which have damaged this vehicle

		//Game timers
		public int _tickCreation;			//The time at which the vehicle was created
		public int _tickUnoccupied;			//The time at which the vehicle was last unoccupied
		public int _tickDead;				//The time at which the vehicle was last dead
        public int _tickControlTime;        //The time at which the vehicle was taken control of
        public int _tickControlEnd;         //How long the duration is

        public int _tickAntiFire;           //The time until fire has been disabled
        public int _tickAntiRotate;         //The time until rotation has been disabled
        public int _tickAntiRecharge;       //TODO: The time until energy regen has been disabled
		#endregion

		#region Events
		public event Action<Vehicle> Destroyed;	//Called when the vehicle has been destroyed
		#endregion

		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		/// <summary>
		/// Is this player currently dead?
		/// </summary>
		public IVehicle Abstract
		{
			get
			{
				_abstract.calculate();
				return _abstract;
			}
		}

		/// <summary>
		/// Is this player currently dead?
		/// </summary>
		public bool IsDead
		{
			get
			{
				return _state.health == 0 || bCondemned;
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

			_childs = new List<Vehicle>();

			_state = new Helpers.ObjectState();
			_attackers = new List<Player>();

			_abstract = new VehicleAbstract(this);
		}

		/// <summary>
		/// Generic constructor
		/// </summary>
		public Vehicle(VehInfo type, Helpers.ObjectState state, Arena arena)
		{	//Populate variables
			_type = type;
			_arena = arena;

			_childs = new List<Vehicle>();

			_state = state;
			_attackers = new List<Player>();

			_abstract = new VehicleAbstract(this);
		}

		/// <summary>
		/// Initialize the state with the default health, energy, etc
		/// </summary>
		public void assignDefaultState()
		{
			_state.health = (short)(_type.Hitpoints == 0 ? 1 : _type.Hitpoints);
			_state.energy = (short)_type.EnergyMax;
		}

		/// <summary>
		/// Determines whether the vehicle is unoccupied and sets the flag if neccessary
		/// </summary>
		public void testForUnoccupied()
		{	//Are we unoccupied?
			if (_tickUnoccupied != 0 && _inhabitant == null)
			{	//Check children
				bool bUnoccupied = true;

				foreach (Vehicle child in _childs)
                    if (child._inhabitant != null)
                    {
                        bUnoccupied = false;
                        _tickUnoccupied = 0;
                    }

				if (bUnoccupied)
					_tickUnoccupied = Environment.TickCount;
			}

			//Check parent
			if (_parent != null)
				_parent.testForUnoccupied();
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
                //no changes
                child._state.yaw = _state.yaw;
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
		public virtual void kill(Player killer)
		{
			kill(killer, 0);
		}

		/// <summary>
		/// Causes the vehicle to die
		/// </summary>
		public virtual void kill(Player killer, int weaponID)
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
		{	//If we're already condemned, abort
			if (bCondemned)
				return;

			//If we have a player, kick him out
			playerLeave(bRestoreBase);

			//Destroy all child vehicles too
			foreach (Vehicle child in _childs)
				child.destroy(bRestoreBase, bRemove);

			//Notify the arena of our destruction
			_arena.lostVehicle(this, bRemove);

			if (Destroyed != null)
				Destroyed(this);
		}
		#endregion

		#region Game State
		/// <summary>
		/// Called when a player is entering the vehicle
		/// </summary>
		public bool playerEnter(Player player)
		{	//If we have an inhabitant already this isn't going to happen
			if (_inhabitant != null)
			{
				Log.write(TLog.Warning, "Player {0} attempted to enter vehicle which already had an inhabitant.", player._alias);
				return false;
			}
			
			//We want to ignore new updates until we
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

            //Reset his movement timer
            player._lastMovement = 0;

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
			_inhabitant._occupiedVehicle = null;
			_inhabitant = null;
            _tickUnoccupied = Environment.TickCount;

            testForUnoccupied();

			Helpers.Object_VehicleBind(_arena.Players, this, null);

		}       

		/// <summary>
		/// Handles damage from explosions triggered nearby
		/// </summary>		
		public virtual void applyExplosion(Player attacker, int dmgX, int dmgY, ItemInfo.Projectile wep)
		{	}

		/// <summary>
		/// Calculates how much damage the vehicle should receive from an explosion
		/// </summary>		
		public void applyExplosionDamage(bool bImpliedExplosion, Player attacker, int dmgX, int dmgY, ItemInfo.Projectile wep)
		{	//Quick check to make sure that the weapon is even damaging before we do math
			int maxRadius = Helpers.getMaxBlastRadius(wep);
			if (maxRadius == 0)
				return;

			//Will this weapon even harm us?
            if (attacker != null)
            {
                switch (wep.damageMode)
                {
                    case 2:			//Enemy
                        if (attacker._team == _team)
                            return;
                        break;

                    case 3:			//Friendly but self
                        if (attacker._team != _team)
                            return;
                        break;

                    case 4:			//Friendly
                        if (attacker._team != _team)
                            return;
                        break;
                }
            }
			//Find the radius at which we were closest to the blast
			double radius = getImpliedRadius(dmgX, dmgY, maxRadius + _type.TriggerRadius);

			radius -= _type.TriggerRadius;
			if (radius < 1)
				radius = 1;
			
			//Calculate the damage for each type
			double fraction = 0;
			double grossDamage = 0;
			double netDamage = 0;

			if (radius <= wep.kineticDamageRadius)
			{
				fraction = 1 - radius / wep.kineticDamageRadius;
				grossDamage = (wep.kineticDamageInner - wep.kineticDamageOuter) * fraction + wep.kineticDamageOuter;
				netDamage += (grossDamage - _type.Armors[0].SelfIgnore) * (1.0d - _type.Armors[0].SelfReduction / 1000.0d);
			}

			if (radius <= wep.explosiveDamageRadius)
			{
				fraction = 1 - radius / wep.explosiveDamageRadius;
				grossDamage = (wep.explosiveDamageInner - wep.explosiveDamageOuter) * fraction + wep.explosiveDamageOuter;
				netDamage += (grossDamage - _type.Armors[1].SelfIgnore) * (1.0d - _type.Armors[1].SelfReduction / 1000.0d);
			}

			if (radius <= wep.electronicDamageRadius)
			{
				fraction = 1 - radius / wep.electronicDamageRadius;
				grossDamage = (wep.electronicDamageInner - wep.electronicDamageOuter) * fraction + wep.electronicDamageOuter;
				netDamage += (grossDamage - _type.Armors[2].SelfIgnore) * (1.0d - _type.Armors[2].SelfReduction / 1000.0d);
			}

			if (radius <= wep.psionicDamageRadius)
			{
				fraction = 1 - radius / wep.psionicDamageRadius;
				grossDamage = (wep.psionicDamageInner - wep.psionicDamageOuter) * fraction + wep.psionicDamageOuter;
				netDamage += (grossDamage - _type.Armors[3].SelfIgnore) * (1.0d - _type.Armors[3].SelfReduction / 1000.0d);
			}

			if (radius <= wep.bypassDamageRadius)
			{
				fraction = 1 - radius / wep.bypassDamageRadius;
				grossDamage = (wep.bypassDamageInner - wep.bypassDamageOuter) * fraction + wep.bypassDamageOuter;
				netDamage += (grossDamage - _type.Armors[4].SelfIgnore) * (1.0d - _type.Armors[4].SelfReduction / 1000.0d);
			}

			if (radius <= wep.energyDamageRadius)
			{
				/*fraction = 1 - radius / wep.energyDamageRadius;
				grossDamage = (wep.energyDamageInner - wep.energyDamageOuter) * fraction + wep.energyDamageOuter;
				netDamage += (grossDamage - _type.Armors[5].SelfIgnore) * (1.0d - _type.Armors[5].SelfReduction / 1000.0d);*/

				// if (netDamage > 0)
				// TODO: take care of energy damage
			}

            //anti effects
            if (radius <= wep.antiEffectsRadius)
            {
                if (wep.antiEffectsFire > 0)
                    _tickAntiFire = Environment.TickCount + (wep.antiEffectsFire * 10000);
                if (wep.antiEffectsRecharge > 0)
                    _tickAntiRecharge = Environment.TickCount + (wep.antiEffectsRecharge * 10000);
                if (wep.antiEffectsRotate > 0)
                    _tickAntiRotate = Environment.TickCount + (wep.antiEffectsRotate * 10000);
            }

			//Apply the damage
			if (netDamage > 0)
			{
				_state.health -= (short)Math.Round((netDamage / 1000));
				if (attacker != null)
                    if (!_attackers.Contains(attacker))
					    _attackers.Add(attacker);
			}

			//Have we been killed?
			if (_state.health <= 0)
			{
                if (attacker != null)
                    kill(attacker, wep.id);
                else
                    kill(null, wep.id);
				_state.health = 0;
			}
		}

		/// <summary>
		/// Determines how close the vehicle may have passed near the explosion in recent history
		/// </summary>	
		private float getImpliedRadius(int posX, int posY, int radiusLimit)
		{	//Are we still?
			if (_state.velocityX == 0 && _state.velocityY == 0 && _state.velocityZ == 0)
			{	//Perform a simple calculation
				return (float)Helpers.distanceTo(posX, posY, _state.positionX, _state.positionY);
			}

			//Calculate the start and end of our velocity 'ray' (giving 400ms leeway)
			float rayStartX = _state.positionX - (((float)_state.velocityX) * (float)(400.0 / 10000.0));
			float rayStartY = _state.positionY - (((float)_state.velocityY) * (float)(400.0 / 10000.0));

			return distanceFromLineSegForPoint(posX, posY, rayStartX, rayStartY, _state.positionX, _state.positionY);
		}

		private float distanceFromLineSegForPoint(float x, float y, float x1, float y1, float x2, float y2)
		{
			float A = x - x1;
			float B = y - y1;
			float C = x2 - x1;
			float D = y2 - y1;

			float dot = A * C + B * D;
			float len_sq = C * C + D * D;
			float param = dot / len_sq;

			float xx, yy;

			if (param < 0)
			{
				xx = x1;
				yy = y1;
			}
			else if (param > 1)
			{
				xx = x2;
				yy = y2;
			}
			else
			{
				xx = x1 + param * C;
				yy = y1 + param * D;
			}

			return distance(x, y, xx, yy);
		}

		private float distance(float aX, float aY, float bX, float bY)
		{
			float dx = aX - bX;
			float dy = aY - bY;

			return (float)Math.Sqrt((dx * dx) + (dy * dy));
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
