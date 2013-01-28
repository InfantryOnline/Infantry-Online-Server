// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Collections.Generic;

using Axiom.Math;

namespace Bnoerj.AI.Steering
{
	public class SimpleVehicle : SteerLibrary
	{
		// give each vehicle a unique number
		public readonly int SerialNumber;
		static int serialNumberCounter = 0;

		// Mass (defaults to unity so acceleration=force)
		float mass;

		// size of bounding sphere, for obstacle avoidance, etc.
		float radius;

		// speed along Forward direction. Because local space is
		// velocity-aligned, velocity = Forward * Speed
		float speed;

		// the maximum steering force this vehicle can apply
		// (steering force is clipped to this magnitude)
		float maxForce;

		// the maximum speed this vehicle is allowed to move
		// (velocity is clipped to this magnitude)
		float maxSpeed;

		float curvature;
        Vector3 lastForward;
        Vector3 lastPosition;
        Vector3 smoothedPosition;
		float smoothedCurvature;
		// The acceleration is smoothed
        Vector3 acceleration;

		// constructor
		public SimpleVehicle()
		{
			// set inital state
			Reset();

			// maintain unique serial numbers
			SerialNumber = serialNumberCounter++;
		}

		// reset vehicle state
		public override void Reset()
		{
			// reset LocalSpace state
			ResetLocalSpace();

			// reset SteerLibraryMixin state
			//FIXME: this is really fragile, needs to be redesigned
			base.Reset();

			Mass = 1;          // Mass (defaults to 1 so acceleration=force)
			Speed = 0;         // speed along Forward direction.

			Radius = 0.5f;     // size of bounding sphere

			MaxForce = 0.1f;   // steering force is clipped to this magnitude
			MaxSpeed = 1.0f;   // velocity is clipped to this magnitude

			// reset bookkeeping to do running averages of these quanities
			ResetSmoothedPosition();
			ResetSmoothedCurvature();
			ResetAcceleration();
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

		// apply a given steering force to our momentum,
		// adjusting our orientation to maintain velocity-alignment.
        public void ApplySteeringForce(Vector3 force, float elapsedTime)
		{
			Vector3 adjustedForce = AdjustRawSteeringForce(force, elapsedTime);

			// enforce limit on magnitude of steering force
            Vector3 clippedForce = Vector3Helpers.TruncateLength(adjustedForce, MaxForce);

			// compute acceleration and velocity
			Vector3 newAcceleration = (clippedForce / Mass);
			Vector3 newVelocity = Velocity;

			// damp out abrupt changes and oscillations in steering acceleration
			// (rate is proportional to time step, then clipped into useful range)
			if (elapsedTime > 0)
			{
				float smoothRate = Utilities.Clip(9 * elapsedTime, 0.15f, 0.4f);
				Utilities.BlendIntoAccumulator(smoothRate, newAcceleration, ref acceleration);
			}

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

			// maintain path curvature information
			MeasurePathCurvature(elapsedTime);

			// running average of recent positions
			Utilities.BlendIntoAccumulator(elapsedTime * 0.06f, // QQQ
								  Position,
								  ref smoothedPosition);
		}

		// the default version: keep FORWARD parallel to velocity, change
		// UP as little as possible.
        public virtual void RegenerateLocalSpace(Vector3 newVelocity, float elapsedTime)
		{
			// adjust orthonormal basis vectors to be aligned with new velocity
			if (Speed > 0)
			{
				RegenerateOrthonormalBasisUF(newVelocity / Speed);
			}
		}

		// adjust the steering force passed to applySteeringForce.
		// allows a specific vehicle class to redefine this adjustment.
		// default is to disallow backward-facing steering at low speed.
		// xxx experimental 8-20-02
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

		// apply a given braking force (for a given dt) to our momentum.
		// xxx experimental 9-6-02
		public void ApplyBrakingForce(float rate, float deltaTime)
		{
			float rawBraking = Speed * rate;
			float clipBraking = ((rawBraking < MaxForce) ? rawBraking : MaxForce);
			Speed = (Speed - (clipBraking * deltaTime));
		}

		// predict position of this vehicle at some time in the future
		// (assumes velocity remains constant)
        public override Vector3 PredictFuturePosition(float predictionTime)
		{
			return Position + (Velocity * predictionTime);
		}

		// get instantaneous curvature (since last update)
		public float Curvature
		{
			get { return curvature; }
		}

		// get/reset smoothedCurvature, smoothedAcceleration and smoothedPosition
		public float SmoothedCurvature
		{
			get { return smoothedCurvature; }
		}
		public float ResetSmoothedCurvature()
		{
			return ResetSmoothedCurvature(0);
		}
		public float ResetSmoothedCurvature(float value)
		{
			lastForward = Vector3.Zero;
			lastPosition = Vector3.Zero;
			return smoothedCurvature = curvature = value;
		}

		public override Vector3 Acceleration
		{
			get { return acceleration; }
		}
        public Vector3 ResetAcceleration()
		{
			return ResetAcceleration(Vector3.Zero);
		}
        public Vector3 ResetAcceleration(Vector3 value)
		{
			return acceleration = value;
		}

        public Vector3 SmoothedPosition
		{
			get { return smoothedPosition; }
		}
        public Vector3 ResetSmoothedPosition()
		{
			return ResetSmoothedPosition(Vector3.Zero);
		}
        public Vector3 ResetSmoothedPosition(Vector3 value)
		{
			return smoothedPosition = value;
		}

		// set a random "2D" heading: set local Up to global Y, then effectively
		// rotate about it by a random angle (pick random forward, derive side).
		public void RandomizeHeadingOnXZPlane()
		{
			Up = Vector3.UnitY;
            Forward = Vector3Helpers.RandomUnitVectorOnXZPlane();
			Side = LocalRotateForwardToSide(Forward);
		}

		// measure path curvature (1/turning-radius), maintain smoothed version
		void MeasurePathCurvature(float elapsedTime)
		{
			if (elapsedTime > 0)
			{
				Vector3 dP = lastPosition - Position;
				Vector3 dF = (lastForward - Forward) / dP.Length;
                Vector3 lateral = Vector3Helpers.PerpendicularComponent(dF, Forward);
                float sign = (Vector3.Dot(lateral, Side) < 0) ? 1.0f : -1.0f;
				curvature = lateral.Length * sign;
				Utilities.BlendIntoAccumulator(elapsedTime * 4.0f, curvature, ref smoothedCurvature);
				lastForward = Forward;
				lastPosition = Position;
			}
		}
	}
}
