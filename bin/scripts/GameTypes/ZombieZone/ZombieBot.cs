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
		private Player victim;					//The player we're currently stalking
		private SteeringController steering;	//System for controlling the bot's steering

		private int _stalkRadius = 10000;		//The distance away we'll look for a victim (in pixels?)
		private int _optimalDistance = 10;		//The distance we want to remain at ideally

		private List<Vector3> _path;			//The path to our destination
		private int _pathTarget;				//The next target node of the path
		private int _tickLastPath;				//The time at which we last made a path to the player

		private float _seperation;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public ZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
			: base(	type, state, arena, 
					new SteeringController(type, state, arena))
		{
			Random rnd = new Random();

			_seperation = (float)rnd.NextDouble();
			steering = _movement as SteeringController;

			_weapon.equip(_arena._server._assets.getItemByName("Spit"));
		}

		/// <summary>
		/// Looks after the bot's functionality
		/// </summary>
		public override bool poll()
		{	//Dead? Do nothing
			if (IsDead)
			{
				steering.steerDelegate = null;
				return base.poll();
			}

			int now = Environment.TickCount;

			//Get the closest player
			victim = getClosestPlayer();

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
						_path = null;

						ThreadPool.QueueUserWorkItem(
							delegate(object state)
							{
								_path = findPathToPlayer(victim);
								_pathTarget = 0;

								_tickLastPath = now;
							}
						);
					}

					//Navigate to him
					if (_path == null)
						//We can't get to him... wander about?
						steering.steerDelegate = null;
					else
						steering.steerDelegate = steerAlongPath;
				}
			}

			//Handle normal functionality
			return base.poll();
		}

		/// <summary>
		/// Finds a path to the given player
		/// </summary>
		private List<Vector3> findPathToPlayer(Player target)
		{	//Perform some pathfinding..
			int[] path;

			bool bSuccess = _arena._pathfinder.calculatePath(
				(short)(_state.positionX / 16), (short)(_state.positionY / 16),
				(short)(target._state.positionX / 16), (short)(target._state.positionY / 16),
				out path);

			if (!bSuccess)
				return null;

			//Transform it into a path we can steer along
			return _arena._pathfinder.createSteerablePath(path);
		}

		/// <summary>
		/// Obtains the nearest valid player
		/// </summary>
		private Player getClosestPlayer()
		{
			List<Player> inTrackingRange =
				_arena.getPlayersInRange(_state.positionX, _state.positionY, _stalkRadius);

			if (inTrackingRange.Count == 0)
				return null;

			//Sort by distance to bot
			inTrackingRange.Sort(
				delegate(Player p, Player q)
				{
					return Comparer<double>.Default.Compare(
						Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, q._state));
				}
			);

			//Get a valid player!
			foreach (Player player in inTrackingRange)
				if (!player.IsDead)
					return player;

			return null;
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
