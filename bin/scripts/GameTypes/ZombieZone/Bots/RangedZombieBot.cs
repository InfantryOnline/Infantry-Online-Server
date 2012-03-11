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
	// RangedZombieBot Class
	/// A ranged zombie which tries to keep it's distance from the player to shoot from afar
	///////////////////////////////////////////////////////
	public class RangedZombieBot : ZombieBot
	{	// Member variables
		///////////////////////////////////////////////////
		public float farDist;				//The distance from the player where we actively pursue them
		public float shortDist;			//The distance from the player where we keep our distance
		public float runDist;				//The distance from the player where we run away!


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public RangedZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz)
			: base(type, state, arena, _zz)
		{
			bOverriddenPoll = true;
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
				{	//What is our distance to the target?
					double distance = (_state.position() - victim._state.position()).Length;
					bool bFleeing = false;

					//Too far?
					if (distance > farDist)
						steering.steerDelegate = steerForPersuePlayer;
					//Too short?
					else if (distance < runDist)
					{
						bFleeing = true;
						steering.steerDelegate = delegate(InfantryVehicle vehicle)
						{
							if (victim != null)
								return vehicle.SteerForFlee(victim._state.position());
							else
								return Vector3.Zero;
						};
					}
					//Quite short?
					else if (distance < shortDist)
					{
						steering.bSkipRotate = true;
						steering.steerDelegate = delegate(InfantryVehicle vehicle)
						{
							if (victim != null)
								return vehicle.SteerForFlee(victim._state.position());
							else
								return Vector3.Zero;
						};
					}
					//Just right
					else
						steering.steerDelegate = null;

					//Can we shoot?
					if (!bFleeing && _weapon.ableToFire())
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
