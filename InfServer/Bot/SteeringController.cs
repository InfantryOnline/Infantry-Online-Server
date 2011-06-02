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
		public bool bSkipRotate;							//Don't turn to steer to the target, just strafe and thrust
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
			double difference;

			if (steerDelegate == null)
			{	//Should we aim anyway?
				if (bSkipAim)
				{
					difference = Helpers.calculateDifferenceInAngles(_state.yaw, angle);

					if (Math.Abs(difference) < 2)
						stopRotating();
					else if (difference > 0)
						rotateRight();
					else
						rotateLeft();
				}
				else
					stopRotating();

				stopThrusting();
				stopStrafing();

				return base.updateState(delta);
			}

			vehicle.Position = _state.position();

			//Determine our steering vector
			Vector3 steer = steerDelegate(vehicle);
			float degrees;

			//Which direction are we supposed to be travelling in?
			double yaw = Utility.ATan2(steer.x, steer.y);
			degrees = -((Utility.RadiansToDegrees(yaw) / 1.5f) - 120);

			double steerDifference = Helpers.calculateDifferenceInAngles(_state.yaw, degrees);

			if (bSkipAim)
				difference = Helpers.calculateDifferenceInAngles(_state.yaw, angle);
			else
				difference = steerDifference;

			if (!bSkipRotate || bSkipAim)
			{	//Turn to face our target
				if (Math.Abs(difference) < 2)
					stopRotating();
				else if (difference > 0)
					rotateRight();
				else
					rotateLeft();
			}
			else
				stopRotating();

			//Move appropriately towards the target
			if (steer.Length < 0.6f)
			{
				stopThrusting();
				stopStrafing();
			}
			else if (steerDifference > 90 || steerDifference < -90)
			{
				thrustBackward();
				stopStrafing();
			}
			else if (steerDifference > 50)
			{
				stopThrusting();
				strafeRight();
			}
			else if (steerDifference < -50)
			{
				stopThrusting();
				strafeLeft();
			}
			else
			{
				thrustForward();
				stopStrafing();
			}
			
			//These should be updated every poll
			bSkipAim = false;
			bSkipRotate = false;

			return base.updateState(delta);
		}

		#region Utility functions

		#endregion
	}
}