using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Axiom.Math;


namespace InfServer.Script.FollowBot
{	// Script Class
	/// Provides the interface between the script and bot
	///////////////////////////////////////////////////////
	class Script_Follow : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Bot _bot;						//Pointer to our bot class

		//private Player victim;					//The player we're currently stalking

		private int _stalkRadius = 1000;		//The distance away we'll look for a victim (in pixels?)
		//private int _optimalDistance = 60;		//The distance we want to remain at ideally

		private int _tickLastShot;

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Performs script initialization
		/// </summary>
		public bool init(IEventObject invoker)
		{	//Populate our variables
			_bot = invoker as Bot;
			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
		public bool poll()
		{
            //Are we dead?
            if (_bot.IsDead)
            {
                _bot.destroy(true);
                return false;
            }

            //Get the closest player
			Player player = getClosestPlayer();
			
			if (player != null && !player.IsDead)
			{	//Move towards him!
				double degrees = Helpers.calculateDegreesBetweenPoints(
					player._state.positionX, player._state.positionY, 
					_bot._state.positionX, _bot._state.positionY);

				double difference = Helpers.calculateDifferenceInAngles(_bot._state.yaw, degrees);

				if (Math.Abs(difference) < 5)
					_bot._movement.stopRotating();
				else if (difference > 0)
					_bot._movement.rotateRight();
				else
					_bot._movement.rotateLeft();

				Vector2 distanceVector = new Vector2(
					_bot._state.positionX - player._state.positionX, 
					_bot._state.positionY - player._state.positionY);
				_bot._movement.stopThrusting();
				Real distLength = distanceVector.Length;

				if (distLength > 150)
					_bot._movement.thrustForward();
				else if (distLength < 50)
					_bot._movement.thrustBackward();
				else
				{	//We're close enough, shoot at them!
					if (Environment.TickCount - _tickLastShot > 1200)
					{
						_bot._itemUseID = 1063;
						_tickLastShot = Environment.TickCount;
					}
				}
			}

			//Delegate updating to the controller
			return false;
		}

		/// <summary>
		/// Obtains the nearest valid player
		/// </summary>
		private Player getClosestPlayer()
		{
			List<Player> inTrackingRange =
				_bot._arena.getPlayersInRange(_bot._state.positionX, _bot._state.positionY, _stalkRadius);

            //Ignore dead players
            inTrackingRange = inTrackingRange.Where(plyr => plyr.IsDead == false).ToList();

			if (inTrackingRange.Count == 0)
				return null;

			// Sort by distance to bot
			inTrackingRange.Sort(
				delegate(Player p, Player q)
				{
					return Comparer<double>.Default.Compare(
						Helpers.distanceSquaredTo(_bot._state, p._state), Helpers.distanceSquaredTo(_bot._state, q._state));
				}
			);

			return inTrackingRange[0];
		}
	}
}