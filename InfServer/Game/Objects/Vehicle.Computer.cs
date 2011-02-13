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
	// Computer Class
	/// Represents a computer vehicle
	///////////////////////////////////////////////////////
	public class Computer : Vehicle
	{	// Member variables
		///////////////////////////////////////////////////
		public new VehInfo.Computer _type;				//The type of vehicle we represent

		public byte _fireAngle;							//The angle we're to fire at
		public bool _shouldFire;						//Should we fire?
		public bool _sendUpdate;						//Should we send an update?

		private int _tickShotTime;						//The last time at which we fired a shot
		private int _tickReloadTime;					//The last time at which we started reloading
		private int _ammoRemaining;						//The amount of ammo we have remaining in our clip

		//Turret settings
		public ItemInfo _primaryGun;					//The weapon we're using to shoot
		public ItemInfo.Projectile _primaryProjectile;	//The main projectile we're considering for aiming
		private int _reloadTime;						//The time taken to reload
		private int _ammoType;							//Ammo settings
		private int _ammoCount;							//
		private int _ammoCapacity;						//


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Computer(VehInfo.Computer type, Arena arena)
			: base(type, arena)
		{
			_type = type;

			//Load the appropriate turret settings
			AssetManager am = Helpers._server._assets;

			//What is the gun we're going to be shooting?			
			int wepID = type.InventoryItems[0];

			if (wepID != 0)
			{	//Find our primary weapon
				_primaryGun = am.getItemByID(wepID);
				_primaryGun.getAmmoType(out _ammoType, out _ammoCount, out _ammoCapacity);
				_ammoRemaining = _ammoCapacity;

				//What sort of 
				if (_primaryGun is ItemInfo.MultiUse)
				{
					ItemInfo.MultiUse mug = (ItemInfo.MultiUse)_primaryGun;
					_reloadTime = mug.reloadDelayNormal * 10;

					//Assume the first item is the tracking projectile
					_primaryProjectile = ((ItemInfo.Projectile)am.getItemByID(mug.childItems[0].id));
				}
				else if (_primaryGun is ItemInfo.Projectile)
				{
					_primaryProjectile = (ItemInfo.Projectile)_primaryGun;
					_reloadTime = _primaryProjectile.reloadDelayNormal * 10;
				}
				else
					//Treat it as no weapon
					_primaryGun = null;
			}
			else
			{
				_primaryGun = null;
			}
		}

		/// <summary>
		/// Keeps the vehicle state updated, and sends an update packet if necessary
		/// </summary>
		/// <returns>A boolean indicating whether an update packet should be sent</returns>
		public bool poll()
		{	//If it isn't a turret then do nothing
			if (_primaryGun == null) 
				return false;

			//If not reloaded yet don't fire
			if (_tickShotTime + (_primaryProjectile.fireDelay * 10) > Environment.TickCount ||
				_tickReloadTime + (_primaryProjectile.reloadDelayNormal * 10) > Environment.TickCount) 
				return false;

			//If below non-operational HP don't fire
			if (_state.health < _type.HitpointsRequiredToOperate) 
				return false;

			//See if there are any valid targets within the tracking radius
			Player target = getClosestValidTarget();
			if (target == null) 
				return false;

			_state.fireAngle = Helpers.computeLeadFireAngle(_state, target._state, _primaryProjectile.muzzleVelocity / 1000);
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
		/// Returns the closest valid target, if any
		/// </summary>		
		private Player getClosestValidTarget()
		{
			List<Player> inTrackingRange = _arena.getPlayersInRange(
			_state.positionX, _state.positionY, _type.TrackingRadius);

			// Sort by distance to turret
			inTrackingRange.Sort(delegate (Player p, Player q)
			{				
				return Comparer<double>.Default.Compare(squaredDistanceTo(p), squaredDistanceTo(q));
			}
			);

			foreach (Player p in inTrackingRange)
			{
				// Since we used the box to look for players, quit if we're looking beyond tracking range (in corners)				
				if (isValidTarget(p)) return p;
			}

			return null;
		}

		/// <summary>
		/// Compute if a player is a valid target that satisfies the turret's config parameters
		/// </summary>		
		private bool isValidTarget(Player p)
		{					
			// Don't fire at spectators
			if (p.IsSpectator) return false;

			// Don't fire at teammates
			if (p._team == _team) return false;

			// Don't fire at dead people
			if (p.IsDead) return false;			

			//Don't fire at people outside of our weight limits.
            int pWeight = p.ActiveVehicle._type.Weight;
            bool trackPlayer = (pWeight >= _type.TrackingWeightLow && pWeight <= _type.TrackingWeightHigh);
            if (!trackPlayer)
                return false;

			// TODO: do not fire at people out of turret angle limits

			// TODO: do not fire at people out of LOS (complicated calculation)
			
			return true;
			
		}

		/// <summary>
		/// Returns squared norm distance from turret to this player
		/// </summary>		
		private double squaredDistanceTo(Player p)
		{
			return (p._state.positionX - _state.positionX) ^ 2 + (p._state.positionY - _state.positionY) ^ 2;
		}

		/// <summary>
		/// Applies damage from each of the six damage types
		/// Destroys the turret if necessary
		/// </summary>		
		public void applyDamage(int dmgX, int dmgY, ItemInfo.Projectile wep)
		{
			double radius = Math.Sqrt((dmgX - _state.positionX)^2 + (dmgY - _state.positionY)^2);

			// NOTE: Damage values are all multiplied by 1000

			double fraction;
			double grossDamage;
			double netDamage;

			if (radius <= wep.kineticDamageRadius)
			{				
				fraction = 1 - radius / wep.kineticDamageRadius;
				grossDamage = (wep.kineticDamageInner - wep.kineticDamageOuter) * fraction + wep.kineticDamageOuter;
				netDamage = (grossDamage - _type.Armors[0].SelfIgnore) * (1.0d - _type.Armors[0].SelfReduction / 1000.0d);
				netDamage /= 1000;
				if (netDamage > 0) _state.health -= (short) Math.Round(netDamage);
			}

			if (radius <= wep.explosiveDamageRadius)
			{
				fraction = 1 - radius / wep.explosiveDamageRadius;
				grossDamage = (wep.explosiveDamageInner - wep.explosiveDamageOuter) * fraction + wep.explosiveDamageOuter;
				netDamage = (grossDamage - _type.Armors[1].SelfIgnore) * (1.0d - _type.Armors[1].SelfReduction / 1000.0d);
				netDamage /= 1000;
				if (netDamage > 0) _state.health -= (short)Math.Round(netDamage);
			}

			if (radius <= wep.electronicDamageRadius)
			{
				fraction = 1 - radius / wep.electronicDamageRadius;
				grossDamage = (wep.electronicDamageInner - wep.electronicDamageOuter) * fraction + wep.electronicDamageOuter;
				netDamage = (grossDamage - _type.Armors[2].SelfIgnore) * (1.0d - _type.Armors[2].SelfReduction / 1000.0d);
				netDamage /= 1000;
				if (netDamage > 0) _state.health -= (short)Math.Round(netDamage);
			}

			if (radius <= wep.psionicDamageRadius)
			{
				fraction = 1 - radius / wep.psionicDamageRadius;
				grossDamage = (wep.psionicDamageInner - wep.psionicDamageOuter) * fraction + wep.psionicDamageOuter;
				netDamage = (grossDamage - _type.Armors[3].SelfIgnore) * (1.0d - _type.Armors[3].SelfReduction / 1000.0d);
				netDamage /= 1000;
				if (netDamage > 0) _state.health -= (short)Math.Round(netDamage);
			}

			if (radius <= wep.bypassDamageRadius)
			{
				fraction = 1 - radius / wep.bypassDamageRadius;
				grossDamage = (wep.bypassDamageInner - wep.bypassDamageOuter) * fraction + wep.bypassDamageOuter;
				netDamage = (grossDamage - _type.Armors[4].SelfIgnore) * (1.0d - _type.Armors[4].SelfReduction / 1000.0d);
				netDamage /= 1000;
				if (netDamage > 0) _state.health -= (short)Math.Round(netDamage);
			}

			if (radius <= wep.energyDamageRadius)
			{
				fraction = 1 - radius / wep.energyDamageRadius;
				grossDamage = (wep.energyDamageInner - wep.energyDamageOuter) * fraction + wep.energyDamageOuter;
				netDamage = (grossDamage - _type.Armors[5].SelfIgnore) * (1.0d - _type.Armors[5].SelfReduction / 1000.0d);
				netDamage /= 1000;

				// if (netDamage > 0)
				// TODO: take care of energy damage
			}

			//Clamp health
			if (_state.health < 0) 
				_state.health = 0;

			//Did we die?
			if (_state.health <= 0)
			{
				_tickDead = Environment.TickCount;

				//Computer vehicles don't linger, so destroy it
				destroy(false);
			}

			_sendUpdate = true;
		}
		
	}
}
