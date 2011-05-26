using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Bots
{
	// SteeringController Class
	/// Maintains the bot's object state based on simple movement instructions
	///////////////////////////////////////////////////////
	public class SteeringController : MovementController
	{	// Member variables
		///////////////////////////////////////////////////
		private InfantryVehicle vehicle;					//Our vehicle to model steering behavior
		public bool bSkipAim;								//Skip aiming and use the given angle
		public float angle;									//The given angle to use

		public Func<InfantryVehicle, Vector3> steerDelegate;//Delegate which calculates how much steering to apply

		
		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic Constructor
		/// </summary>
		public SteeringController(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
			: base(type, state, arena)
		{
			vehicle = new InfantryVehicle(type, _state);

			//Set our position
			vehicle.Position = _state.position();
		}

		/// <summary>
		/// Updates the state in accordance with movement instructions
		/// </summary>
		public override bool updateState(double delta)
		{
			if (steerDelegate == null)
				return base.updateState(delta);

			vehicle.Position = _state.position();

			//Determine our steering vector
			Vector3 steer = steerDelegate(vehicle);
			float degrees;

			if (!bSkipAim)
			{	//Which direction are we supposed to be travelling in?
				double yaw = Utility.ATan2(steer.x, steer.y);
				degrees = -((Utility.RadiansToDegrees(yaw) / 1.5f) - 120);
			}
			else
				degrees = angle;

			double difference = Helpers.calculateDifferenceInAngles(_state.yaw, degrees);

			if (Math.Abs(difference) < 2)
				//Force the angle for the sake of accuracy
				stopRotating();
			else if (difference > 0)
				rotateRight();
			else
				rotateLeft();

			//Move appropriately towards the target
			if (steer.Length < 0.6f)
			{
				stopThrusting();
				stopStrafing();
			}
			else if (difference > 90 || difference < -90)
			{
				thrustBackward();
				stopStrafing();
			}
			else if (difference > 50)
			{
				stopThrusting();
				strafeRight();
			}
			else if (difference < -50)
			{
				stopThrusting();
				strafeLeft();
			}
			else
			{
				thrustForward();
				stopStrafing();
			}
			
			return base.updateState(delta);
		}

		#region Utility functions

		#endregion
	}
}