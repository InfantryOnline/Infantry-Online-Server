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
	// DualZombieBot Class
	/// A zombie with two weapons, used based on range
	///////////////////////////////////////////////////////
	public class DualZombieBot : ZombieBot
	{	// Member variables
		///////////////////////////////////////////////////
		public float wepSwitchDist;
		public bool bNoAimFar;

		private WeaponController _weaponClose;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public DualZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz)
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
					bool bClose = (distance < wepSwitchDist);

					if (bClose)
						weapon = _weaponClose;

					//Can we shoot?
					if (weapon.ableToFire())
					{
						if (bClose || !bNoAimFar)
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
						{
							_itemUseID = weapon.ItemID;
							weapon.shotFired();
						}
					}
					else
						steering.bSkipAim = false;
				}
				else
				{	//Does our path need to be updated?
					if (now - _tickLastPath > Script_ZombieZone.c_zombiePathUpdateInterval)
					{	//Are we close enough to the team?
						if (!checkTeamDistance())
						{
							_path = null;
							destroy(true);
						}
						else
						{	//Update it!
							_tickLastPath = int.MaxValue;

							_arena._pathfinder.queueRequest(
								(short)(_state.positionX / 16), (short)(_state.positionY / 16),
								(short)(victim._state.positionX / 16), (short)(victim._state.positionY / 16),
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
	}
}
