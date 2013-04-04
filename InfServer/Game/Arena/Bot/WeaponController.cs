using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;

namespace InfServer.Bots
{
	// WeaponController Class
	/// Facilitates the use of weapon items for a bot 
	///////////////////////////////////////////////////////
	public class WeaponController
	{	// Member variables
		///////////////////////////////////////////////////
		public bool bEquipped;							//Are we equipped with a weapon?

		private Helpers.ObjectState _state;				//The state of the vehicle we represent
		private WeaponSettings _settings;				//The controller settings

		private ItemInfo _type;							//The type of item weapon we're using
		private ItemInfo _primaryItem;					//The main item we're using
		private ItemInfo.Projectile _primaryProjectile;	//The projectile we're paying attention to

		private int _fireDelay;							//The delay between firing shots
		private int _reloadTime;						//The time taken to reload
		private int _ammoType;							//Ammo settings
		private int _ammoCount;							//
		private int _ammoCapacity;						//

		private int _tickShotTime;						//The last time at which we fired a shot
		private int _tickReloadTime;					//The last time at which we started reloading
		private int _ammoRemaining;						//The amount of ammo we have remaining in our clip

		//Properties
		public ItemInfo.Projectile Projectile
		{
			get
			{
				return _primaryProjectile;
			}
		}

		public short ItemID
		{
			get
			{
				return (short)_type.id;
			}
		}

		public int AmmoUse
		{
			get
			{
				return _ammoCount;
			}
		}

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		/// <summary>
		/// Weapon controller settings class
		/// </summary>
		public class WeaponSettings
		{
			public byte aimFuzziness;			//The leeway we give when aiming at a target

			//Constructor
			public WeaponSettings()
			{
				aimFuzziness = 2;
			}
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic Constructor
		/// </summary>
		public WeaponController(Helpers.ObjectState state, WeaponSettings settings)
		{
			_state = state;
			_settings = settings;

			bEquipped = false;
		}

		/// <summary>
		/// Changes the WeaponController settings
		/// </summary>
		public void setSettings(WeaponSettings settings)
		{
			_settings = settings;
		}

		/// <summary>
		/// Equips the weapon controller with a new item
		/// </summary>
		public bool equip(ItemInfo item)
		{
			_type = item;

			//Find our primary weapon
			AssetManager am = Helpers._server._assets;

			_primaryItem = item;
			_primaryItem.getAmmoType(out _ammoType, out _ammoCount, out _ammoCapacity);
			_ammoRemaining = _ammoCapacity;

			//What sort of item are we dealing with?
			if (_primaryItem is ItemInfo.MultiUse)
			{
				ItemInfo.MultiUse mug = (ItemInfo.MultiUse)_primaryItem;
				_reloadTime = mug.reloadDelayNormal * 10;
				_fireDelay = mug.fireDelay * 10;

				//Assume the first item is the tracking projectile
				_primaryProjectile = ((ItemInfo.Projectile)am.getItemByID(mug.childItems[0].id));
			}
			else if (_primaryItem is ItemInfo.Projectile)
			{
				_primaryProjectile = (ItemInfo.Projectile)_primaryItem;
				_reloadTime = _primaryProjectile.reloadDelayNormal * 10;
				_fireDelay = _primaryProjectile.fireDelay * 10;
			}
			else
			{	//We can't handle this item
				Log.write(TLog.Warning, "Weapon controller given invalid equip item {0}", item);
				return false;
			}

			bEquipped = (_primaryProjectile != null);

			return true;
		}

		/// <summary>
		/// Determines whether the bot is currently able to fire it's weapon
		/// </summary>
		public bool ableToFire()
		{	//If not reloaded yet don't fire
			int now = Environment.TickCount;

			if (_tickShotTime + _fireDelay > now ||
				_tickReloadTime > now)
				return false;

			return true;
		}

		/// <summary>
		/// Notifies the controller that the bot has fired a shot
		/// </summary>
		public void shotFired()
		{	//We've fired a shot!
			int now = Environment.TickCount;
			_tickShotTime = now;

			//Adjust ammo accordingly            
			if (_ammoCapacity != 0 && --_ammoRemaining <= 0)
			{
				_tickReloadTime = now + _reloadTime;
				_ammoRemaining = _ammoCapacity;
			}
		}

		/// <summary>
		/// Retrieves the appropriate angle to aim at to hit the target
		/// </summary>
		public bool isAimed(int aimAngle)
		{	//Calculate the smallest angle difference
			int angleDiffHi = aimAngle - _state.yaw;
			int angleDiffLo = (angleDiffHi < 0) ? 240 - Math.Abs(angleDiffHi) : -(240 - Math.Abs(angleDiffHi));

			int angleDiff;

			if (Math.Abs(angleDiffHi) < Math.Abs(angleDiffLo))
				angleDiff = angleDiffHi;
			else
				angleDiff = angleDiffLo;

			//Is it in range according to our fuzziness factor?
			return (Math.Abs(angleDiff) <= (_settings.aimFuzziness / 2));
		}

		/// <summary>
		/// Retrieves the appropriate angle to aim at to hit the target
		/// </summary>
		public int getAimAngle(Helpers.ObjectState target)
		{
			return Helpers.computeLeadFireAngle(_state, target, _primaryProjectile.muzzleVelocity / 1000);
		}

		/// <summary>
		/// Determines whether the bot needs to aim to clockwise, anticlockwise or is ontarget
		/// </summary>
		public int testAim(Helpers.ObjectState target, out bool bAimed)
		{	//Compute our lead fire angle
			int fireAngle = Helpers.computeLeadFireAngle(_state, target, _primaryProjectile.muzzleVelocity / 1000);

			//Calculate the smallest angle difference
			int angleDiffHi = fireAngle - _state.yaw;
			int angleDiffLo = (angleDiffHi < 0) ? 240 - Math.Abs(angleDiffHi) : -(240 - Math.Abs(angleDiffHi));

			int angleDiff;

			if (Math.Abs(angleDiffHi) < Math.Abs(angleDiffLo))
				angleDiff = angleDiffHi;
			else
				angleDiff = angleDiffLo;

			//Is it in range according to our fuzziness factor?
			bAimed = (Math.Abs(angleDiff) <= (_settings.aimFuzziness / 2));

			//Which direction do we need to aim in?
			if (angleDiff > 0)
				//Aim clockwise
				return 1;
			else
				//Aim anticlockwise
				return -1;
		}
	}
}