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
	// CombatBot Class
	/// A combat drone which attacks the enemies of the engineer
	///////////////////////////////////////////////////////
	public class CombatBot : Bot
	{	// Member variables
		///////////////////////////////////////////////////
		public Player creator;					//The engineer who created us

		private Vehicle targetZombie;

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
		public CombatBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz, Player engineer)
			: base(type, state, arena,
					new SteeringController(type, state, arena))
		{
			Random rnd = new Random();

			_seperation = (float)rnd.NextDouble();
			steering = _movement as SteeringController;

			if (type.InventoryItems[0] != 0)
				_weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

			zz = _zz;
			creator = engineer;
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

			//Do we have a player to follow?
			if (creator == null || creator.IsDead || creator.IsSpectator)
			{
				kill(null);
				return false;
			}

			int now = Environment.TickCount;

			//Get the team state
			Script_ZombieZone.TeamState state = zz.getTeamState(_team);
	
			//Stay close to our owner
			bool bClearPath = Helpers.calcBresenhemsPredicate(_arena,
					_state.positionX, _state.positionY, creator._state.positionX, creator._state.positionY,
					delegate(LvlInfo.Tile t)
					{
						return !t.Blocked;
					}
				);

			if (bClearPath)
				//Persue directly!
				steering.steerDelegate = steerForFollowOwner;
			else
			{	//Does our path need to be updated?
				if (now - _tickLastPath > Script_ZombieZone.c_combatBotPathUpdateInterval)
				{	//Update it!
					_tickLastPath = int.MaxValue;

					_arena._pathfinder.queueRequest(
						(short)(_state.positionX / 16), (short)(_state.positionY / 16),
						(short)(creator._state.positionX / 16), (short)(creator._state.positionY / 16),
						delegate(List<Vector3> path, int pathLength)
						{
							if (path != null)
							{	//Is the path too long?
								if (pathLength > Script_ZombieZone.c_zombieMaxPath)
								{	//Destroy ourself and let another zombie take our place
									_path = null;
									destroy(true);
								}
								else
								{
									_path = path;
									_pathTarget = 1;
								}
							}

							_tickLastPath = now;
						}
					);
				}

				//Navigate to him
				if (_path == null)
					//If we can't find out way to him, just mindlessly walk in his direction for now
					steering.steerDelegate = steerForFollowOwner;
				else
					steering.steerDelegate = steerAlongPath;
			}

			//Do we need to seek a new target?
			if (targetZombie == null || !isValidTarget(targetZombie))
				targetZombie = getTargetZombie(state);

			if (targetZombie != null)
			{
				if (bClearPath)
				{	//Can we shoot?
					if (_weapon.ableToFire())
					{
						int aimResult = _weapon.getAimAngle(targetZombie._state);

						if (_weapon.isAimed(aimResult) && creator.inventoryModify(2000, -_weapon.AmmoUse))
						{	//Spot on! Fire?
							_itemUseID = _weapon.ItemID;
							_weapon.shotFired();

							//Simulate the damage if we're attacking a bot
							if (targetZombie._bBotVehicle)
								targetZombie.applyExplosionDamage(true, creator, targetZombie._state.positionX, targetZombie._state.positionY, _weapon.Projectile);
						}

						steering.bSkipAim = true;
						steering.angle = aimResult;
					}
					else
						steering.bSkipAim = false;
				}
			}

			//Handle normal functionality
			return base.poll();
		}

		/// <summary>
		/// Obtains a suitable target player
		/// </summary>
		protected bool isValidTarget(Vehicle zombie)
		{	//Don't shoot a dead zombie
			if (zombie.IsDead)
				return false;

			//Or phased zombie
			if (zombie._type.Id == 117)
				return false;

			//Is it too far away?
			if (Helpers.distanceTo(this, zombie) > 800)
				return false;

			//We must have vision on it
			bool bVision = Helpers.calcBresenhemsPredicate(_arena,
				_state.positionX, _state.positionY, zombie._state.positionX, zombie._state.positionY,
				delegate(LvlInfo.Tile t)
				{
					return !t.Blocked;
				}
			);

			return bVision;
		}

		/// <summary>
		/// Obtains a suitable target player
		/// </summary>
		protected Vehicle getTargetZombie(Script_ZombieZone.TeamState state)
		{	//Find the closest valid zombie
			Vector3 selfpos = _state.position();
			IEnumerable<ZombieBot> zombies = state.zombies.OrderBy(zomb => zomb._state.position().DistanceSquared(selfpos));

			foreach (ZombieBot zombie in zombies)
				if (isValidTarget(zombie))
					return zombie;

			IEnumerable<Player> zombiePlayers = state.zombiePlayers.OrderBy(zomb => zomb._state.position().DistanceSquared(selfpos));

			foreach (Player zombie in zombiePlayers)
				if (!zombie.IsSpectator && isValidTarget(zombie._baseVehicle))
					return zombie._baseVehicle;

			return null;
		}

		#region Steer Delegates
		/// <summary>
		/// Steers the zombie along the defined path
		/// </summary>
		public Vector3 steerAlongPath(InfantryVehicle vehicle)
		{	//Are we at the end of the path?
			if (_pathTarget >= _path.Count)
			{	//Invalidate the path
				_path = null;
				_tickLastPath = 0;
				return Vector3.Zero;
			}

			//Find the nearest path point
			Vector3 point = _path[_pathTarget];

			//Are we close enough to go to the next?
			if (_pathTarget < _path.Count && vehicle.Position.Distance(point) < 0.8f)
				point = _path[_pathTarget++];

			return vehicle.SteerForSeek(point);
		}

		/// <summary>
		/// Keeps the combat bot around the engineer
		/// </summary>
		public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
		{
			if (creator == null)
				return Vector3.Zero;

			Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
			Vector3 pursuitSteer = vehicle.SteerForPursuit(creator._baseVehicle.Abstract, 0.2f);

			return (wanderSteer * 1.6f) + pursuitSteer;
		}
		#endregion
	}
}
