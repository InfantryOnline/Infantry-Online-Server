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
	// KingZombieBot Class
	/// The king zombie, forever hunting the enemy team
	///////////////////////////////////////////////////////
	public class KingZombieBot : ZombieBot
	{	// Member variables
		///////////////////////////////////////////////////
		private WeaponController _weaponClose;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public KingZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz)
			: base(type, state, arena, _zz)
		{
			bOverriddenPoll = true;
			_weaponClose = new WeaponController(_state, new WeaponController.WeaponSettings());

			//Setup our second weapon
			if (type.InventoryItems[1] != 0)
				_weaponClose.equip(AssetManager.Manager.getItemByID(type.InventoryItems[1]));
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

			if (checkCircumstances())
				return base.poll();

			//Get the closest player
			bool bClearPath = false;
			victim = getTargetPlayer(ref bClearPath);

			if (victim != null)
			{
				if (bClearPath)
				{	//Persue directly!
					steering.steerDelegate = steerForPersuePlayer;

					//Which weapon should we use?
					WeaponController weapon = _weapon;
					double distance = (_state.position() - victim._state.position()).Length;
					bool bClose = (distance < 1.0);

					if (bClose)
						weapon = _weaponClose;

					//Can we shoot?
					if (weapon.ableToFire())
					{
						int aimResult = weapon.getAimAngle(victim._state);

						if (weapon.isAimed(aimResult))
						{	//Spot on! Fire?
							_itemUseID = weapon.ItemID;
							weapon.shotFired();
						}

						steering.bSkipAim = true;
						steering.angle = aimResult;
					}
					else
						steering.bSkipAim = false;
				}
				else
				{	//Does our path need to be updated?
					if (now - _tickLastPath > Script_ZombieZone.c_zombiePathUpdateInterval)
					{	//Update it!
						_tickLastPath = int.MaxValue;

						_arena._pathfinder.queueRequest(
							(short)(_state.positionX / 16), (short)(_state.positionY / 16),
							(short)(victim._state.positionX / 16), (short)(victim._state.positionY / 16),
							delegate(List<Vector3> path, int pathLength)
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
		/// Checks for any distractions we should approach
		/// </summary>
		protected override bool checkForDistractions(Script_ZombieZone.TeamState state)
		{	//We don't get distracted!
			return false;
		}
	}
}
