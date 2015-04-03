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

		protected int _tickLastUpdate;					//The last time at which we sent an update packet
		protected int _tickShotTime;					//The last time at which we fired a shot
		protected int _tickReloadTime;					//The last time at which we started reloading
        protected int _ammoRemaining;					//The amount of ammo we have remaining in our clip

		//Turret settings
		public ItemInfo _primaryGun;					//The weapon we're using to shoot
		public ItemInfo.Projectile _primaryProjectile;	//The main projectile we're considering for aiming
        public List<ItemInfo.UtilityItem> _activeEquip;	//Our active equipment
		protected int _fireDelay;						//The delay between firing shots
		protected int _reloadTime;						//The time taken to reload
		protected int _ammoType;						//Ammo settings
		protected int _ammoCount;						//
		protected int _ammoCapacity;					//

        protected double _getHealth;                    //For health repair - sends update when reaches a whole number
        protected double _getEnergy;                    //For energy repair


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
            //_arena.sendArenaMessage("wepid: " + wepID);

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
					_fireDelay = mug.fireDelay * 10;

					//Assume the first item is the tracking projectile
					_primaryProjectile = ((ItemInfo.Projectile)am.getItemByID(mug.childItems[0].id));
				}
				else if (_primaryGun is ItemInfo.Projectile)
				{
					_primaryProjectile = (ItemInfo.Projectile)_primaryGun;
					_reloadTime = _primaryProjectile.reloadDelayNormal * 10;
					_fireDelay = _primaryProjectile.fireDelay * 10;
				}
				else
					//Treat it as no weapon
					_primaryGun = null;
			}
			else
			{
				_primaryGun = null;
			}

            configureComp();
		}

        /// <summary>
        /// Configures the computer based on the vehicle information
        /// </summary>
        protected virtual void configureComp()
        {	//Are we using any equipment?
            _activeEquip = new List<ItemInfo.UtilityItem>();
            foreach (int item in _type.InventoryItems)
            {
                if (item != 0)
                {
                    ItemInfo.UtilityItem util = AssetManager.Manager.getItemByID(item) as ItemInfo.UtilityItem;
                    if (util == null)
                    {
                        //Log.write(TLog.Inane, "configureComp(): computer inventory item ({0}) not found", item);
                        continue;
                    }
                    else
                        _activeEquip.Add(util);
                }
            }
        }

		/// <summary>
		/// Keeps the vehicle state updated, and sends an update packet if necessary
		/// </summary>
		/// <returns>A boolean indicating whether an update packet should be sent</returns>
		public virtual bool poll()
		{	//If it isn't a turret then send an update tick every now and then for health
			int now = Environment.TickCount;

			if (_primaryGun == null)
			{
				if (now - _tickLastUpdate > 2000)
				{
					_tickLastUpdate = now;
					return true;
				}

				return false;
			}

			//If below non-operational HP don't fire
			if (_state.health < _type.HitpointsRequiredToOperate) 
				return false;

            //Check repair rate
            int tick = now - _tickLastUpdate;
            if (_type.RepairRate > 0 && tick >= (_type.RepairRate * 10))
            {
                short check = _state.health;
                double _incHealth = ((double)_type.RepairRate / 100);
                double temp = _incHealth;
                _getHealth -= ((int)temp - _incHealth);
                if ((check + (int)_incHealth) > _state.health)
                {
                    //Added hp bonus every other tick
                    if (_getHealth >= 1)
                    {
                        _state.health = (short)Math.Min(_type.Hitpoints, _state.health + (int)_incHealth + (int)_getHealth);
                        _getHealth = 0;
                    }
                    else
                        _state.health = (short)Math.Min(_type.Hitpoints, _state.health + (int)_incHealth);
                }
            }

            //If below required amount of energy, don't fire
            if (_type.ComputerEnergyMax > 0 && _state.energy < _type.ComputerEnergyMax)
                return false;

            //Check energy rate
            if (_type.EnergyMax > 0 && _type.ComputerEnergyRate > 0 && tick >= (_type.ComputerEnergyRate * 10))
            {
                short check = _state.energy;
                double _incEnergy = ((double)_type.ComputerEnergyRate / 100);
                double temp = _incEnergy;
                _getEnergy -= ((int)temp - _incEnergy);
                if ((check + (int)_incEnergy) > _state.energy)
                {
                    //Added energy bonus every other tick
                    if (_getEnergy >= 1)
                    {
                        _state.energy = (short)Math.Min(_type.EnergyMax, _state.energy + (int)_incEnergy + (int)_getEnergy);
                        _getEnergy = 0;
                    }
                    else
                        _state.energy = (short)Math.Min(_type.EnergyMax, _state.energy + (int)_incEnergy);
                }
            }

			//Are we a peaceful vehicle?
			if (_team._id == -1)
				return false;

			//See if there are any valid targets within the tracking radius
			Player target = getClosestValidTarget();
            if (target == null)
                return false;

			//Look at our target if we're allowed to rotate
            if(_tickAntiRotate < now)
			    _state.fireAngle = Helpers.computeLeadFireAngle(_state, target._state, _primaryProjectile.muzzleVelocity / 1000);

			//If not reloaded yet don't fire
			if (_tickShotTime + _fireDelay > now ||
				_tickReloadTime > now || _tickAntiFire > now)
			{	//But maybe send an update packet?
				if (now - _tickLastUpdate > 300)
				{
					_tickLastUpdate = now;
					return true;
				}

				return false;
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
		/// Returns the closest valid target, if any
		/// </summary>		
		protected Player getClosestValidTarget()
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
                if (p == null)
                    continue;

				// Since we used the box to look for players, quit if we're looking beyond tracking range (in corners)
				if (isValidTarget(p)) return p;
			}

			return null;
		}

		/// <summary>
		/// Compute if a player is a valid target that satisfies the turret's config parameters
		/// </summary>		
		protected virtual bool isValidTarget(Player p)
		{
            if (p == null)
            {
                Log.write(TLog.Error, "isValidTarget(): Called with null player.");
                return false;   //I dunno -X15
            }

			// Don't fire at spectators
			if (p.IsSpectator) return false;

			// Don't fire at teammates
            if (p._team == _team && (_owner == null || _owner == p._team)) return false;

            //Don't fire at temporary teammates
            if (_owner == p._team) return false;

			// Don't fire at dead people
			if (p.IsDead) return false;

            // Don't fire at people outside of turret's firing range
            if (!InRange(p)) return false;

			//Don't fire at people outside of our weight limits.
            int pWeight = p.ActiveVehicle._type.Weight;
            bool trackPlayer = (pWeight >= _type.TrackingWeightLow && pWeight <= _type.TrackingWeightHigh);
            if (!trackPlayer)
                return false;

			// Do not fire at people out of turret angle limits
            double pAngle = Helpers.calcDegreesBetweenPoints(p._state.positionX, p._state.positionY, _state.positionX, _state.positionY);
            if (_type.AngleStart > 0)
                if ((short)pAngle < _type.AngleStart)
                    return false;

            if (_type.AngleLength < 360)
                if ((short)pAngle > _type.AngleLength)
                    return false;

            //Check if player is within turrets vision
            if (_type.ObeyLos != 0)
            {
                if (IsPlayerOccluded(p))
                    return false;
            }

			return true;
		}

        /// <summary>
        /// Returns true if the player is hidden from the vehicle's view, likely by a tile.
        /// </summary>
        /// <param name="p">player to check</param>
        /// <returns>true if player cannot be seen</returns>
		protected Boolean IsPlayerOccluded(Player p)
        {
            if (p == null || p._arena == null)
            {
                Log.write(TLog.Error, "IsPlayerOccluded(): Called with null player and / or arena.");
                return true; //True? False? I dunno -X15
            }

            //Pretty ugly way to get it done but it works perfectly. Vision tiles are all calculated seemingly fine. If you have a better way of doing it, by all means. - Super-man
            int tileSize = 8; //8 is nonexistant.
            int tileDistance = 0;
            List<LvlInfo.Tile> tiles = Helpers.calcBresenhems(p._arena, _state.positionX, _state.positionY, p._state.positionX, p._state.positionY);
            for (int i = 0; i <= tiles.Count - 1; i++)
            {
                if (tiles[i].Vision > 0)
                {
                    if (tiles[i].Vision <= tileSize)
                    {
                        //Take note of this vision tile
                        tileSize = tiles[i].Vision;
                        tileDistance = i;
                    }
                }
            }
            if (tileSize == 8 || tiles.Count < Math.Floor(tileSize * 1.8) + tileDistance)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the player is in turret's firing range.
        /// </summary>
        /// <param name="p">Player to check</param>
        /// <returns>true if player is in firing range</returns>
		protected Boolean InRange(Player p)
		{
			double d = Math.Sqrt(squaredDistanceTo(p));

			if (d <= _type.FireRadius)
				return true;

			return false;
		}

		/// <summary>
		/// Returns squared norm distance from turret to this player
		/// </summary>		
		protected double squaredDistanceTo(Player p)
		{
			return Math.Pow(p._state.positionX - _state.positionX, 2) + Math.Pow(p._state.positionY - _state.positionY, 2);
		}

		/// <summary>
		/// Causes the vehicle to die
		/// </summary>
		public override void kill(Player killer)
		{	//Set our health to 0
			_state.health = 0;
			_tickDead = Environment.TickCount;

            _arena._turretGroups.Remove(this);

			//Computer vehicles don't linger, so destroy it
			destroy(true);
		}        

		/// <summary>
		/// Handles damage from explosions triggered nearby
		/// </summary>		
		public override void applyExplosion(Player attacker, int dmgX, int dmgY, ItemInfo.Projectile wep)
        {   //Apply our damage
			applyExplosionDamage(false, attacker, dmgX, dmgY, wep);

			//Did we die?
			if (_state.health <= 0)
			{	//Are we destroyable?
                if (_type.Destroyable == 0)
                    //We arent, set health to 0
                    _state.health = 0;
                else
                    //Computer vehicles don't linger, so destroy it
                   kill(null);
			}

			_sendUpdate = true;
		}
	}
}