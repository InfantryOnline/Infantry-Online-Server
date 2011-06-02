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

			//Get the closest player
			victim = getTargetPlayer();

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
					steering.steerDelegate = delegate(InfantryVehicle vehicle)
					{	//A simple pursuit path!
						if (victim != null)
							return vehicle.SteerForPursuit(victim._baseVehicle.Abstract, 0);
						else
							return Vector3.Zero;
					};
					
					//If we're close enough... explode!
					double distance = (_state.position() - victim._state.position()).Length;
					if (distance < 0.5f)
						kill(null);
				}
				else
				{	//Does our path need to be updated?
					if (now - _tickLastPath > 10000)
					{	//Update it!
						_tickLastPath = int.MaxValue;

						_arena._pathfinder.queueRequest(
							(short)(_state.positionX / 16), (short)(_state.positionY / 16),
							(short)(victim._state.positionX / 16), (short)(victim._state.positionY / 16),
							delegate(List<Vector3> path)
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
	}
}
