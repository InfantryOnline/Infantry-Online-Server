using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Axiom.Math;
using Assets;

namespace InfServer.Script.GameType_ZombieZone
{
	// LaserTurret Class
	/// A laser turret which shoots at zombies
	///////////////////////////////////////////////////////
	public class LaserTurret : Computer
	{	// Member variables
		///////////////////////////////////////////////////
		private Vehicle targetZombie;
		public Script_ZombieZone zz;

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public LaserTurret(VehInfo.Computer type, Arena arena)
			: base(type, arena)
		{
		}

		/// <summary>
		/// Keeps the vehicle state updated, and sends an update packet if necessary
		/// </summary>
		/// <returns>A boolean indicating whether an update packet should be sent</returns>
		public override bool poll()
		{	//If it isn't a turret then do nothing
			if (_primaryGun == null)
				return false;

			//If below non-operational HP don't fire
			if (_state.health < _type.HitpointsRequiredToOperate)
				return false;

			if (zz == null)
				return false;

			//Make sure our creator is still on our team
			if (_creator._team == null || _creator._team != _team)
				return false;

			//Get the team state
			Script_ZombieZone.TeamState state = zz.getTeamState(_team);

			//Do we need to seek a new target?
			if (targetZombie == null || !isValidTarget(targetZombie))
				targetZombie = getTargetZombie(state);

			//Valid target?
			if (targetZombie == null)
				return false;

			//Look at our target!
			_state.fireAngle = Helpers.computeLeadFireAngle(_state, targetZombie._state, _primaryProjectile.muzzleVelocity / 1000);

			//If not reloaded yet don't fire
			int now = Environment.TickCount;

			if (_tickShotTime + _fireDelay > now ||
				_tickReloadTime > now)
			{	//But maybe send an update packet?
				if (now - _tickLastUpdate > 300)
				{
					_tickLastUpdate = now;
					return true;
				}

				return false;
			}

			//We're firing! On a bot?
			if (targetZombie._bBotVehicle)
			{
				targetZombie.applyExplosionDamage(true, _creator,
					targetZombie._state.positionX, targetZombie._state.positionY, _primaryProjectile);
			}
			
			_shouldFire = true;
			_tickShotTime = Environment.TickCount;

			//Adjust ammo accordingly
			if (_ammoCapacity != 0 && --_ammoRemaining <= 0)
			{
				_tickReloadTime = Environment.TickCount + _reloadTime;
				_ammoRemaining = _ammoCapacity;
			}

			return true;
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
			if (Helpers.distanceTo(this, zombie) > _type.TrackingRadius)
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
	}
}
