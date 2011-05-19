using System;
using System.Collections.Generic;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;

namespace InfServer.Script.GameType_ZombieZone
{
	// ZombieBot Class
	/// A simple zombie-type bot
	///////////////////////////////////////////////////////
	public class ZombieBot : Bot
	{	// Member variables
		///////////////////////////////////////////////////
		private Player victim;					//The player we're currently stalking

		private int _stalkRadius = 1000;		//The distance away we'll look for a victim (in pixels?)
		private int _optimalDistance = 10;		//The distance we want to remain at ideally

		private int _tickLastShot;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public ZombieBot(VehInfo.Car type, Arena arena)
			: base(type, arena)
		{
		}

		/// <summary>
		/// Generic constructor
		/// </summary>
		public ZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
			: base(type, state, arena)
		{
		}

		/// <summary>
		/// Looks after the bot's functionality
		/// </summary>
		public override bool poll()
		{	//Dead? Do nothing
			if (IsDead)
				return base.poll();

			//Get the closest player
			Player player = getClosestPlayer();
			
			if (player != null)
			{	//Move towards him!
				double degrees = Helpers.calculateDegreesBetweenPoints(
					player._state.positionX, player._state.positionY, 
					_state.positionX, _state.positionY);

				double difference = Helpers.calculateDifferenceInAngles(_state.yaw, degrees);

				if (Math.Abs(difference) < 5)
					_movement.stopRotating();
				else if (difference > 0)
					_movement.rotateRight();
				else
					_movement.rotateLeft();

				Vector2 distanceVector = new Vector2(
					_state.positionX - player._state.positionX, 
					_state.positionY - player._state.positionY);
				_movement.stopThrusting();
				Real distLength = distanceVector.Length;

				if (distLength > 150)
					_movement.thrustForward();
				else if (distLength < 50)
					_movement.thrustBackward();
				else
				{	//We're close enough, shoot at them!
					if (Environment.TickCount - _tickLastShot > 1200)
					{
						_itemUseID = 1063;
						_tickLastShot = Environment.TickCount;
					}
				}
			}

			//Handle normal functionality
			return base.poll();
		}

		/// <summary>
		/// Obtains the nearest valid player
		/// </summary>
		private Player getClosestPlayer()
		{
			List<Player> inTrackingRange =
				_arena.getPlayersInRange(_state.positionX, _state.positionY, _stalkRadius);

			if (inTrackingRange.Count == 0)
				return null;

			//Sort by distance to bot
			inTrackingRange.Sort(
				delegate(Player p, Player q)
				{
					return Comparer<double>.Default.Compare(
						Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, q._state));
				}
			);

			//Get a valid player!
			foreach (Player player in inTrackingRange)
				if (!player.IsDead)
					return player;

			return null;
		}
	}
}
