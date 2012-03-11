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
	// SuicideZombieBot Class
	/// A suicidal zombie which attempts to get close to the player and then explodes
	///////////////////////////////////////////////////////
	public class SuicideZombieBot : ZombieBot
	{	// Member variables
		///////////////////////////////////////////////////



		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public SuicideZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz)
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
				{	//Persue directly!
					steering.steerDelegate = delegate(InfantryVehicle vehicle)
					{	//A simple pursuit path!
						if (victim != null)
							return vehicle.SteerForPursuit(victim._baseVehicle.Abstract, 0);
						else
							return Vector3.Zero;
					};
					
					//If we're close enough... explode!
					double distance = (_state.position() - victim._state.position()).Length;
					if (distance < 0.6f)
						kill(null);
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
