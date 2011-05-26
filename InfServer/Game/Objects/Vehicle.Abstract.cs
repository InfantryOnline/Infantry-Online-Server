using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Game
{
	// VehicleAbstract Class
	/// Provides an open steer interface to related to vehicles
	///////////////////////////////////////////////////////
	public class VehicleAbstract : AbstractVehicle
	{	// Member variables
		///////////////////////////////////////////////////
		private Vehicle owner;
		private Helpers.ObjectState state;

		private float mass;								//Mass (defaults to unity so acceleration=force)
		private float radius;							//Size of bounding sphere, for obstacle avoidance, etc.
		private float speed;							//Speed along Forward direction
		private float maxForce;							//The maximum steering force this vehicle can apply
		private float maxSpeed;							//The maximum speed at which this vehicle is allowed to move


		#region Accessors
		public override Vector3 Acceleration
		{
			get { return Vector3.Zero; }
		}

		// get/set Mass
		public override float Mass
		{
			get { return mass; }
			set { mass = value; }
		}

		// get velocity of vehicle
		public override Vector3 Velocity
		{
			get { return state.velocity(); }
		}

		// get/set speed of vehicle  (may be faster than taking mag of velocity)
		public override float Speed
		{
			get { return speed; }
			set { speed = value; }
		}

		// size of bounding sphere, for obstacle avoidance, etc.
		public override float Radius
		{
			get { return radius; }
			set { radius = value; }
		}

		// get/set maxForce
		public override float MaxForce
		{
			get { return maxForce; }
			set { maxForce = value; }
		}

		// get/set maxSpeed
		public override float MaxSpeed
		{
			get { return maxSpeed; }
			set { maxSpeed = value; }
		}
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public VehicleAbstract(Vehicle _owner)
		{
			owner = _owner;
		}

		/// <summary>
		/// Calculates the AbstractVehicle fields based on the object state given
		/// </summary>
		public void calculate()
		{
			state = owner._state;

			Position = state.position();

			//Calculate the local space
			speed = state.velocity().Normalize();

			if (speed > 0)
				RegenerateOrthonormalBasisUF(state.velocity() / speed);
		}

		/// <summary>
		/// Predict position of this vehicle at some time in the future
		/// </summary>
		public override Vector3 PredictFuturePosition(float predictionTime)
		{
			return Position + (Velocity * predictionTime);
		}
	}
}
