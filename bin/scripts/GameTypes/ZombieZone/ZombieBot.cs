using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_ZombieZone
{
	// ZombieBot Class
	/// A simple zombie-type bot
	///////////////////////////////////////////////////////
	public class ZombieBot : Bot
	{	// Member variables
		///////////////////////////////////////////////////
		public Team targetTeam;					//The team of which players we're targetting

		protected bool bOverriddenPoll;			//Do we have custom actions for poll?

		protected Player victim;				//The player we're currently stalking
		protected SteeringController steering;	//System for controlling the bot's steering
		protected Script_ZombieZone zz;			//The zombiezone script

		protected List<Vector3> _path;			//The path to our destination
		protected int _pathTarget;				//The next target node of the path
		protected int _tickLastPath;			//The time at which we last made a path to the player

		private float _seperation;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public ZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz)
			: base(	type, state, arena, 
					new SteeringController(type, state, arena))
		{
			Random rnd = new Random();

			_seperation = (float)rnd.NextDouble();
			steering = _movement as SteeringController;

			if (type.InventoryItems[0] != 0)
				_weapon.equip(_arena._server._assets.getItemByID(type.InventoryItems[0]));

			zz = _zz;
		}

		/// <summary>
		/// Looks after the bot's functionality
		/// </summary>
		public override bool poll()
		{	//Overridden?
			if (bOverriddenPoll)
				return base.poll();
			
			//Dead? Do nothing
			if (IsDead)
			{
				steering.steerDelegate = null;
				return base.poll();
			}

			int now = Environment.TickCount;

			//Get the closest player
			victim = getTargetPlayer();

			if (victim != null)
			{	//Do we have a direct path to the player?
				bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, 
					_state.positionX, _state.positionY, victim._state.positionX, victim._state.positionY,
					delegate(LvlInfo.Tile t)
					{
						return !t.Blocked;
					}
				);

				if (bClearPath)
				{	//Persue directly!
					steering.steerDelegate = steerForPersuePlayer;

					//Can we shoot?
					if (_weapon.ableToFire())
					{
						int aimResult = _weapon.getAimAngle(victim._state);

						if (_weapon.isAimed(aimResult))
						{	//Spot on! Fire?
							_itemUseID = _weapon.ItemID;
							_weapon.shotFired();
						}

						steering.bSkipAim = true;
						steering.angle = aimResult;
					}
					else
						steering.bSkipAim = false;
				}
				else
				{	//Does our path need to be updated?
					if (now - _tickLastPath > 10000)
					{	//Update it!
						_tickLastPath = int.MaxValue;

						_arena._pathfinder.queueRequest(
							(short)(_state.positionX / 16), (short)(_state.positionY / 16),
							(short)(victim._state.positionX / 16), (short)(victim._state.positionY / 16),
							delegate(List<Vector3> path)
							{
								if (path != null)
								{
									_path = path;
									_pathTarget = 1;
								}

								_tickLastPath = now;
							}
						);
					}

					//Navigate to him
					if (_path == null)
						//If we can't find out way to him, just mindlessly walk in his direction for now
						steering.steerDelegate = steerForPersuePlayer;
					else
						steering.steerDelegate = steerAlongPath;
				}
			}

			//Handle normal functionality
			return base.poll();
		}

		/// <summary>
		/// Obtains a suitable target player
		/// </summary>
		protected Player getTargetPlayer()
		{	//Look at the players on the target team
			if (targetTeam == null)
				return null;

			Player target = null;
			double lastDist = double.MaxValue;

			foreach (Player p in targetTeam.ActivePlayers)
			{	//Find the closest player
				if (p.IsDead)
					continue;

				double dist = Helpers.distanceSquaredTo(_state, p._state);

				if (lastDist > dist)
				{
					lastDist = dist;
					target = p;
				}
			}

			return target;
		}

		#region Steer Delegates
		/// <summary>
		/// Steers the zombie along the defined path
		/// </summary>
		public Vector3 steerAlongPath(InfantryVehicle vehicle)
		{	//Find the nearest path point
			Vector3 point = _path[_pathTarget];

			//Are we close enough to go to the next?
			if (_pathTarget != _path.Count - 1 && vehicle.Position.Distance(point) < 1.0f)
				point = _path[_pathTarget++];

			return vehicle.SteerForSeek(point);
		}

		/// <summary>
		/// Moves the zombie on a persuit course towards the player, while keeping seperated from other zombies
		/// </summary>
		public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
		{
			if (victim == null)
				return Vector3.Zero;

			List<Vehicle> zombies = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 400, 
																delegate(Vehicle v)
																{	return (v is ZombieBot);	});
			IEnumerable<IVehicle> zombiebots = zombies.ConvertAll<IVehicle>(
				delegate(Vehicle v)
				{
					return (v as ZombieBot).Abstract;
				}
			);

			Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, zombiebots);
			Vector3 pursuitSteer = vehicle.SteerForPursuit(victim._baseVehicle.Abstract, 0.2f);

			return (seperationSteer * 0.6f) + pursuitSteer;
		}
		#endregion
	}
}
