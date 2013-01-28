using System;
using System.Collections.Generic;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;

namespace InfServer.Bots
{
	// FollowBot Class
	/// Follows the nearest player around with a manical peristance
	///////////////////////////////////////////////////////
	public class FollowBot : Bot
	{	// Member variables
		///////////////////////////////////////////////////
		private int _tickLastPoll;					//The last tick at which poll was called

		private Player victim;						//The player we're currently stalking

		private int stalkRadius = 1000;				//The distance away we'll look for a victim (in pixels?)
		private int optimalDistance = 100;			//The distance we want to remain at ideally


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public FollowBot(VehInfo.Car type, Arena arena)
			: base(type, arena)
		{
		}

		/// <summary>
		/// Generic constructor
		/// </summary>
		public FollowBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
			: base(type, state, arena)
		{
		}

		#region State
		/// <summary>
		/// Looks after the bot's functionality
		/// </summary>
		public override bool poll()
		{	//Calculate our delta time..
			int tickCount = Environment.TickCount;
			int delta = tickCount - _tickLastPoll;

			_tickLastPoll = tickCount;

			//Get the closest player
			Player player = getClosestPlayer();
			if (player != null)
			{	//Move towards him!
				double degrees = Helpers.calculateDegreesBetweenPoints(player._state.positionX, player._state.positionY, _state.positionX, _state.positionY);
				double difference = Helpers.calculateDifferenceInAngles(_state.yaw, degrees);

				if (Math.Abs(difference) < 5)
					_controller.stopRotating();
				else if (difference > 0)
					_controller.rotateRight();
				else
					_controller.rotateLeft();

				Vector2 distanceVector = new Vector2(_state.positionX - player._state.positionX, _state.positionY - player._state.positionY);
				_controller.stopThrusting();

				if (distanceVector.magnitude() > 150)
					_controller.thrustForward();
				else if (distanceVector.magnitude() < 50)
					_controller.thrustBackward();
			}

			//Allow the bot to function
			return base.poll();
		}
		#endregion

		#region Utility
		/// <summary>
		/// Obtains the nearest valid player
		/// </summary>
		private Player getClosestPlayer()
		{
			List<Player> inTrackingRange = _arena.getPlayersInRange(
			_state.positionX, _state.positionY, stalkRadius);

			if (inTrackingRange.Count == 0)
				return null;

			// Sort by distance to bot
			inTrackingRange.Sort(
				delegate(Player p, Player q)
				{
					return Comparer<double>.Default.Compare(
						Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, p._state));
				}
			);

			return inTrackingRange[0];
		}
		#endregion
	}
}
