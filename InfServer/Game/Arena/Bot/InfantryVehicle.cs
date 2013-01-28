using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;
using Bnoerj.AI.Steering;
using Axiom.Math;

namespace InfServer.Bots
{
	// InfantryVehicle Class
	/// Represents a steering-handled infantry vehicle/bot
	///////////////////////////////////////////////////////
	public class InfantryVehicle : SteerLibrary
	{	// Member variables
		///////////////////////////////////////////////////
		private VehInfo.Car vehicleType;				//The type of the vehicle we're driving
		public Helpers.ObjectState state;				//The state of the object we're controlling

		private float mass;								//Mass (defaults to unity so acceleration=force)
		private float radius;							//Size of bounding sphere, for obstacle avoidance, etc.
		private float speed;							//Speed along Forward direction
		private float maxForce;							//The maximum steering force this vehicle can apply
		private float maxSpeed;							//The maximum speed at which this vehicle is allowed to move
		
		private Vector3 acceleration;					//Our acceleration

		public Func<InfantryVehicle, Vector3> steerDelegate;	//Delegate which calculates how much steering to apply

		#region Accessors
		public override Vector3 Acceleration
		{
			get { return acceleration; }
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
			get { return Forward * speed; }
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
		/// Generic Constructor
		/// </summary>
		public InfantryVehicle(VehInfo.Car type, Helpers.ObjectState _state)
		{
			vehicleType = type;
			state = _state;

			Reset();

			SpeedValues stats = vehicleType.TerrainSpeeds[0];

			MaxForce = ((stats.RollThrust * 4800) / 128) / 1000;
			MaxSpeed = stats.RollTopSpeed / 1000; 
		}

		/// <summary>
		/// Resets the vehicle state
		/// </summary>
		public override void Reset()
		{
			ResetLocalSpace();
			
			//FIXME: this is really fragile, needs to be redesigned
			base.Reset();

			Mass = 1;          // Mass (defaults to 1 so acceleration=force)
			Speed = 0;         // speed along Forward direction.

			Radius = 0.5f;     // size of bounding sphere
		}

		/// <summary>
		/// Applies a given steering force to our momentum
		/// </summary>
		public void ApplySteeringForce(Vector3 force, float elapsedTime)
		{
			Vector3 adjustedForce = AdjustRawSteeringForce(force, elapsedTime);

			// enforce limit on magnitude of steering force
			Vector3 clippedForce = Vector3Helpers.TruncateLength(adjustedForce, MaxForce);

			// compute acceleration and velocity
			acceleration = (clippedForce / Mass);
			Vector3 newVelocity = Velocity;

			// Euler integrate (per frame) acceleration into velocity
			newVelocity += acceleration * elapsedTime;

			// enforce speed limit
			newVelocity = Vector3Helpers.TruncateLength(newVelocity, MaxSpeed);

			// update Speed
			Speed = (newVelocity.Length);

			// Euler integrate (per frame) velocity into position
			Position = (Position + (newVelocity * elapsedTime));

			// regenerate local space (by default: align vehicle's forward axis with
			// new velocity, but this behavior may be overridden by derived classes.)
			RegenerateLocalSpace(newVelocity, elapsedTime);
		}

		/// <summary>
		/// Changes our orientation so that the speed vector direction is 'forward'
		/// </summary>
		public virtual void RegenerateLocalSpace(Vector3 newVelocity, float elapsedTime)
		{
			// adjust orthonormal basis vectors to be aligned with new velocity
			if (Speed > 0)
			{
				RegenerateOrthonormalBasisUF(newVelocity / Speed);
			}
		}

		/// <summary>
		/// Adjusts the steering force passed to ApplySteeringForce
		/// </summary>
		public virtual Vector3 AdjustRawSteeringForce(Vector3 force, float deltaTime)
		{
			float maxAdjustedSpeed = 0.2f * MaxSpeed;

			if ((Speed > maxAdjustedSpeed) || (force == Vector3.Zero))
			{
				return force;
			}
			else
			{
				float range = Speed / maxAdjustedSpeed;
				float cosine = Utilities.Interpolate((float)Math.Pow(range, 20), 1.0f, -1.0f);
				return Vector3Helpers.LimitMaxDeviationAngle(force, cosine, Forward);
			}
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
