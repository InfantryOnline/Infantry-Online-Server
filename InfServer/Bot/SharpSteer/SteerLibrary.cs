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
	//FIXME: this class should not be abstract
	public abstract class SteerLibrary : AbstractVehicle
	{
		// Constructor: initializes state
		public SteerLibrary()
		{
			// set inital state
			Reset();
		}

		// reset state
		public virtual void Reset()
		{
			// initial state of wander behavior
			WanderSide = 0;
			WanderUp = 0;

			// default to non-gaudyPursuitAnnotation
			GaudyPursuitAnnotation = false;
		}

		// -------------------------------------------------- steering behaviors

		// Wander behavior
		public float WanderSide;
		public float WanderUp;

        public Vector3 SteerForWander(float dt)
		{
			// random walk WanderSide and WanderUp between -1 and +1
			float speed = 12 * dt; // maybe this (12) should be an argument?
			WanderSide = Utilities.ScalarRandomWalk(WanderSide, speed, -1, +1);
			WanderUp = Utilities.ScalarRandomWalk(WanderUp, speed, -1, +1);

			// return a pure lateral steering vector: (+/-Side) + (+/-Up)
			return (this.Side * WanderSide) + (this.Up * WanderUp);
		}

		// Seek behavior
        public Vector3 SteerForSeek(Vector3 target)
		{
            Vector3 desiredVelocity = target - this.Position;
			return desiredVelocity - this.Velocity;
		}

		// Flee behavior
        public Vector3 SteerForFlee(Vector3 target)
		{
            Vector3 desiredVelocity = this.Position - target;
			return desiredVelocity - this.Velocity;
		}

		// xxx proposed, experimental new seek/flee [cwr 9-16-02]
        public Vector3 xxxSteerForFlee(Vector3 target)
		{
			//  const Vector3 offset = position - target;
            Vector3 offset = this.Position - target;
            Vector3 desiredVelocity = Vector3Helpers.TruncateLength(offset, this.MaxSpeed); //xxxnew
			return desiredVelocity - this.Velocity;
		}

        public Vector3 xxxSteerForSeek(Vector3 target)
		{
			//  const Vector3 offset = target - position;
            Vector3 offset = target - this.Position;
            Vector3 desiredVelocity = Vector3Helpers.TruncateLength(offset, this.MaxSpeed); //xxxnew
			return desiredVelocity - this.Velocity;
		}

		// Path Following behaviors
        public Vector3 SteerToFollowPath(int direction, float predictionTime, Pathway path)
		{
			// our goal will be offset from our path distance by this amount
			float pathDistanceOffset = direction * predictionTime * this.Speed;

			// predict our future position
            Vector3 futurePosition = this.PredictFuturePosition(predictionTime);

			// measure distance along path of our current and predicted positions
			float nowPathDistance = path.MapPointToPathDistance(this.Position);
			float futurePathDistance = path.MapPointToPathDistance(futurePosition);

			// are we facing in the correction direction?
			bool rightway = ((pathDistanceOffset > 0) ?
								   (nowPathDistance < futurePathDistance) :
								   (nowPathDistance > futurePathDistance));

			// find the point on the path nearest the predicted future position
			// XXX need to improve calling sequence, maybe change to return a
			// XXX special path-defined object which includes two Vector3s and a 
			// XXX bool (onPath,tangent (ignored), withinPath)
            Vector3 tangent;
			float outside;
            Vector3 onPath = path.MapPointToPath(futurePosition, out tangent, out outside);

			// no steering is required if (a) our future position is inside
			// the path tube and (b) we are facing in the correct direction
			if ((outside < 0) && rightway)
			{
				// all is well, return zero steering
				return Vector3.Zero;
			}
			else
			{
				// otherwise we need to steer towards a target point obtained
				// by adding pathDistanceOffset to our current path position

				float targetPathDistance = nowPathDistance + pathDistanceOffset;
                Vector3 target = path.MapPathDistanceToPoint(targetPathDistance);

				// return steering to seek target on path
				return SteerForSeek(target);
			}
		}

        public Vector3 SteerToStayOnPath(float predictionTime, Pathway path)
		{
			// predict our future position
            Vector3 futurePosition = this.PredictFuturePosition(predictionTime);

			// find the point on the path nearest the predicted future position
            Vector3 tangent;
			float outside;
            Vector3 onPath = path.MapPointToPath(futurePosition, out tangent, out outside);

			if (outside < 0)
			{
				// our predicted future position was in the path,
				// return zero steering.
				return Vector3.Zero;
			}
			else
			{
				// our predicted future position was outside the path, need to
				// steer towards it.  Use onPath projection of futurePosition
				// as seek target
				return SteerForSeek(onPath);
			}
		}

		// ------------------------------------------------------------------------
		// Obstacle Avoidance behavior
		//
		// Returns a steering force to avoid a given obstacle.  The purely
		// lateral steering force will turn our this towards a silhouette edge
		// of the obstacle.  Avoidance is required when (1) the obstacle
		// intersects the this's current path, (2) it is in front of the
		// this, and (3) is within minTimeToCollision seconds of travel at the
		// this's current velocity.  Returns a zero vector value (Vector3::zero)
		// when no avoidance is required.
        public Vector3 SteerToAvoidObstacle(float minTimeToCollision, IObstacle obstacle)
		{
            Vector3 avoidance = obstacle.SteerToAvoid(this, minTimeToCollision);

			return avoidance;
		}

		// avoids all obstacles in an ObstacleGroup
        public Vector3 SteerToAvoidObstacles<Obstacle>(float minTimeToCollision, List<Obstacle> obstacles)
			where Obstacle : IObstacle
		{
            Vector3 avoidance = Vector3.Zero;
			PathIntersection nearest = new PathIntersection();
			PathIntersection next = new PathIntersection();
			float minDistanceToCollision = minTimeToCollision * this.Speed;

			next.intersect = false;
			nearest.intersect = false;

			// test all obstacles for intersection with my forward axis,
			// select the one whose point of intersection is nearest
			foreach (Obstacle o in obstacles)
			{
				//FIXME: this should be a generic call on Obstacle, rather than this code which presumes the obstacle is spherical
				FindNextIntersectionWithSphere(o as SphericalObstacle, ref next);

				if (nearest.intersect == false || (next.intersect != false && next.distance < nearest.distance))
					nearest = next;
			}

			// when a nearest intersection was found
			if ((nearest.intersect != false) && (nearest.distance < minDistanceToCollision))
			{
				// compute avoidance steering force: take offset from obstacle to me,
				// take the component of that which is lateral (perpendicular to my
				// forward direction), set length to maxForce, add a bit of forward
				// component (in capture the flag, we never want to slow down)
                Vector3 offset = this.Position - nearest.obstacle.Center;
                avoidance = Vector3Helpers.PerpendicularComponent(offset, this.Forward);
				avoidance.Normalize();
				avoidance *= this.MaxForce;
				avoidance += this.Forward * this.MaxForce * 0.75f;
			}

			return avoidance;
		}

		// ------------------------------------------------------------------------
		// Unaligned collision avoidance behavior: avoid colliding with other
		// nearby vehicles moving in unconstrained directions.  Determine which
		// (if any) other other this we would collide with first, then steers
		// to avoid the site of that potential collision.  Returns a steering
		// force vector, which is zero length if there is no impending collision.
        public Vector3 SteerToAvoidNeighbors<TVehicle>(float minTimeToCollision, IEnumerable<TVehicle> others)
			where TVehicle : IVehicle
		{
			// first priority is to prevent immediate interpenetration
            Vector3 separation = SteerToAvoidCloseNeighbors(0, others);
			if (separation != Vector3.Zero) return separation;

			// otherwise, go on to consider potential future collisions
			float steer = 0;
			IVehicle threat = null;

			// Time (in seconds) until the most immediate collision threat found
			// so far.  Initial value is a threshold: don't look more than this
			// many frames into the future.
			float minTime = minTimeToCollision;

			// xxx solely for annotation
            Vector3 xxxThreatPositionAtNearestApproach = Vector3.Zero;
            Vector3 xxxOurPositionAtNearestApproach = Vector3.Zero;

			// for each of the other vehicles, determine which (if any)
			// pose the most immediate threat of collision.
			foreach (IVehicle other in others)
			{
				if (other != this/*this*/)
				{
					// avoid when future positions are this close (or less)
					float collisionDangerThreshold = this.Radius * 2;

					// predicted time until nearest approach of "this" and "other"
					float time = PredictNearestApproachTime(other);

					// If the time is in the future, sooner than any other
					// threatened collision...
					if ((time >= 0) && (time < minTime))
					{
						// if the two will be close enough to collide,
						// make a note of it
						if (ComputeNearestApproachPositions(other, time) < collisionDangerThreshold)
						{
							minTime = time;
							threat = other;
							xxxThreatPositionAtNearestApproach = hisPositionAtNearestApproach;
							xxxOurPositionAtNearestApproach = ourPositionAtNearestApproach;
						}
					}
				}
			}

			// if a potential collision was found, compute steering to avoid
			if (threat != null)
			{
				// parallel: +1, perpendicular: 0, anti-parallel: -1
                float parallelness = Vector3.Dot(this.Forward, threat.Forward);
				float angle = 0.707f;

				if (parallelness < -angle)
				{
					// anti-parallel "head on" paths:
					// steer away from future threat position
                    Vector3 offset = xxxThreatPositionAtNearestApproach - this.Position;
                    float sideDot = Vector3.Dot(offset, this.Side);
					steer = (sideDot > 0) ? -1.0f : 1.0f;
				}
				else
				{
					if (parallelness > angle)
					{
						// parallel paths: steer away from threat
						Vector3 offset = threat.Position - this.Position;
                        float sideDot = Vector3.Dot(offset, this.Side);
						steer = (sideDot > 0) ? -1.0f : 1.0f;
					}
					else
					{
						// perpendicular paths: steer behind threat
						// (only the slower of the two does this)
						if (threat.Speed <= this.Speed)
						{
                            float sideDot = Vector3.Dot(this.Side, threat.Velocity);
							steer = (sideDot > 0) ? -1.0f : 1.0f;
						}
					}
				}
			}

			return this.Side * steer;
		}

		// Given two vehicles, based on their current positions and velocities,
		// determine the time until nearest approach
		public float PredictNearestApproachTime(IVehicle other)
		{
			// imagine we are at the origin with no velocity,
			// compute the relative velocity of the other this
            Vector3 myVelocity = this.Velocity;
            Vector3 otherVelocity = other.Velocity;
            Vector3 relVelocity = otherVelocity - myVelocity;
			float relSpeed = relVelocity.Length;

			// for parallel paths, the vehicles will always be at the same distance,
			// so return 0 (aka "now") since "there is no time like the present"
			if (relSpeed == 0) return 0;

			// Now consider the path of the other this in this relative
			// space, a line defined by the relative position and velocity.
			// The distance from the origin (our this) to that line is
			// the nearest approach.

			// Take the unit tangent along the other this's path
            Vector3 relTangent = relVelocity / relSpeed;

			// find distance from its path to origin (compute offset from
			// other to us, find length of projection onto path)
            Vector3 relPosition = this.Position - other.Position;
            float projection = Vector3.Dot(relTangent, relPosition);

			return projection / relSpeed;
		}

		// Given the time until nearest approach (predictNearestApproachTime)
		// determine position of each this at that time, and the distance
		// between them
		public float ComputeNearestApproachPositions(IVehicle other, float time)
		{
            Vector3 myTravel = this.Forward * this.Speed * time;
            Vector3 otherTravel = other.Forward * other.Speed * time;

            Vector3 myFinal = this.Position + myTravel;
            Vector3 otherFinal = other.Position + otherTravel;

			// xxx for annotation
			ourPositionAtNearestApproach = myFinal;
			hisPositionAtNearestApproach = otherFinal;

			return Vector3.Distance(myFinal, otherFinal);
		}

		/// XXX globals only for the sake of graphical annotation
        Vector3 hisPositionAtNearestApproach;
        Vector3 ourPositionAtNearestApproach;

		// ------------------------------------------------------------------------
		// avoidance of "close neighbors" -- used only by steerToAvoidNeighbors
		//
		// XXX  Does a hard steer away from any other agent who comes withing a
		// XXX  critical distance.  Ideally this should be replaced with a call
		// XXX  to steerForSeparation.
        public Vector3 SteerToAvoidCloseNeighbors<TVehicle>(float minSeparationDistance, IEnumerable<TVehicle> others)
			where TVehicle : IVehicle
		{
			// for each of the other vehicles...
			foreach (IVehicle other in others)
			{
				if (other != this/*this*/)
				{
					float sumOfRadii = this.Radius + other.Radius;
					float minCenterToCenter = minSeparationDistance + sumOfRadii;
					Vector3 offset = other.Position - this.Position;
					float currentDistance = offset.Length;

					//If we're exactly on top of each other, something's gotta give
					if (offset == Vector3.Zero)
					{
						Random rnd = new Random();
						return new Vector3(rnd.NextDouble(), rnd.NextDouble(), 0);
					}

					if (currentDistance < minCenterToCenter)
					{	
                        return Vector3Helpers.PerpendicularComponent(-offset, this.Forward);
					}
				}
			}

			// otherwise return zero
			return Vector3.Zero;
		}

		// ------------------------------------------------------------------------
		// used by boid behaviors
		public bool IsInBoidNeighborhood(IVehicle other, float minDistance, float maxDistance, float cosMaxAngle)
		{
			if (other == this)
			{
				return false;
			}
			else
			{
                Vector3 offset = other.Position - this.Position;
				float distanceSquared = offset.LengthSquared;

				// definitely in neighborhood if inside minDistance sphere
				if (distanceSquared < (minDistance * minDistance))
				{
					return true;
				}
				else
				{
					// definitely not in neighborhood if outside maxDistance sphere
					if (distanceSquared > (maxDistance * maxDistance))
					{
						return false;
					}
					else
					{
						// otherwise, test angular offset from forward axis
                        Vector3 unitOffset = offset / (float)Math.Sqrt(distanceSquared);
                        float forwardness = Vector3.Dot(this.Forward, unitOffset);
						return forwardness > cosMaxAngle;
					}
				}
			}
		}

		// ------------------------------------------------------------------------
		// Separation behavior -- determines the direction away from nearby boids
        public Vector3 SteerForSeparation(float maxDistance, float cosMaxAngle, IEnumerable<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
            Vector3 steering = Vector3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			foreach (IVehicle other in flock)
			{
				if (IsInBoidNeighborhood(other, this.Radius * 3, maxDistance, cosMaxAngle))
				{
					// add in steering contribution
					// (opposite of the offset direction, divided once by distance
					// to normalize, divided another time to get 1/d falloff)
					Vector3 offset = other.Position - this.Position;
                    Real distanceSquared = Vector3.Dot(offset, offset);
					if (distanceSquared == 0.0f)
						continue;

					steering += (offset / -distanceSquared);

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, then normalize to pure direction
            if (neighbors > 0)
            {
                steering = (steering / (float)neighbors);
                steering.Normalize();
            }

			return steering;
		}

		// ------------------------------------------------------------------------
		// Alignment behavior
        public Vector3 SteerForAlignment(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vector3 steering = Vector3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, this.Radius * 3, maxDistance, cosMaxAngle))
				{
					// accumulate sum of neighbor's heading
					steering += other.Forward;

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, subtract off current heading to get error-
			// correcting direction, then normalize to pure direction
            if (neighbors > 0)
            {
                steering = ((steering / (float)neighbors) - this.Forward);
                steering.Normalize();
            }

			return steering;
		}

		// ------------------------------------------------------------------------
		// Cohesion behavior
        public Vector3 SteerForCohesion(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vector3 steering = Vector3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, this.Radius * 3, maxDistance, cosMaxAngle))
				{
					// accumulate sum of neighbor's positions
					steering += other.Position;

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, subtract off current position to get error-
			// correcting direction, then normalize to pure direction
			if (neighbors > 0)
            {
                steering = ((steering / (float)neighbors) - this.Position);
                steering.Normalize();
            }

			return steering;
		}

		// ------------------------------------------------------------------------
		// pursuit of another this (& version with ceiling on prediction time)
        public Vector3 SteerForPursuit(IVehicle quarry)
		{
			return SteerForPursuit(quarry, float.MaxValue);
		}

        public Vector3 SteerForPursuit(IVehicle quarry, float maxPredictionTime)
		{
			// offset from this to quarry, that distance, unit vector toward quarry
            Vector3 offset = quarry.Position - this.Position;
			Real distance = offset.Length;
			if (distance == 0.0f)
				return Vector3.Zero;

            Vector3 unitOffset = offset / distance;
			
			// how parallel are the paths of "this" and the quarry
			// (1 means parallel, 0 is pependicular, -1 is anti-parallel)
            float parallelness = Vector3.Dot(this.Forward, quarry.Forward);

			// how "forward" is the direction to the quarry
			// (1 means dead ahead, 0 is directly to the side, -1 is straight back)
            float forwardness = Vector3.Dot(this.Forward, unitOffset);

			float directTravelTime = distance / this.Speed;
			int f = Utilities.IntervalComparison(forwardness, -0.707f, 0.707f);
			int p = Utilities.IntervalComparison(parallelness, -0.707f, 0.707f);

			float timeFactor = 0;   // to be filled in below

			// Break the pursuit into nine cases, the cross product of the
			// quarry being [ahead, aside, or behind] us and heading
			// [parallel, perpendicular, or anti-parallel] to us.
			switch (f)
			{
			case +1:
				switch (p)
				{
				case +1:          // ahead, parallel
					timeFactor = 4;
					break;
				case 0:           // ahead, perpendicular
					timeFactor = 1.8f;
					break;
				case -1:          // ahead, anti-parallel
					timeFactor = 0.85f;
					break;
				}
				break;
			case 0:
				switch (p)
				{
				case +1:          // aside, parallel
					timeFactor = 1;
					break;
				case 0:           // aside, perpendicular
					timeFactor = 0.8f;
					break;
				case -1:          // aside, anti-parallel
					timeFactor = 4;
					break;
				}
				break;
			case -1:
				switch (p)
				{
				case +1:          // behind, parallel
					timeFactor = 0.5f;
					break;
				case 0:           // behind, perpendicular
					timeFactor = 2;
					break;
				case -1:          // behind, anti-parallel
					timeFactor = 2;
					break;
				}
				break;
			}

			// estimated time until intercept of quarry
			float et = directTravelTime * timeFactor;

			// xxx experiment, if kept, this limit should be an argument
			float etl = (et > maxPredictionTime) ? maxPredictionTime : et;

			// estimated position of quarry at intercept
			Vector3 target = quarry.PredictFuturePosition(etl);

			return SteerForSeek(target);
		}

		// for annotation
		public bool GaudyPursuitAnnotation;

		// ------------------------------------------------------------------------
		// evasion of another this
        public Vector3 SteerForEvasion(IVehicle menace, float maxPredictionTime)
		{
			// offset from this to menace, that distance, unit vector toward menace
			Vector3 offset = menace.Position - this.Position;
			float distance = offset.Length;

			float roughTime = distance / menace.Speed;
			float predictionTime = ((roughTime > maxPredictionTime) ? maxPredictionTime : roughTime);

			Vector3 target = menace.PredictFuturePosition(predictionTime);

			return SteerForFlee(target);
		}

		// ------------------------------------------------------------------------
		// tries to maintain a given speed, returns a maxForce-clipped steering
		// force along the forward/backward axis
        public Vector3 SteerForTargetSpeed(float targetSpeed)
		{
			float mf = this.MaxForce;
			float speedError = targetSpeed - this.Speed;
			return this.Forward * Utilities.Clip(speedError, -mf, +mf);
		}

		// ----------------------------------------------------------- utilities
		// XXX these belong somewhere besides the steering library
		// XXX above AbstractVehicle, below SimpleVehicle
		// XXX ("utility this"?)

		// xxx cwr experimental 9-9-02 -- names OK?
        public bool IsAhead(Vector3 target)
		{
			return IsAhead(target, 0.707f);
		}
        public bool IsAside(Vector3 target)
		{
			return IsAside(target, 0.707f);
		}
        public bool IsBehind(Vector3 target)
		{
			return IsBehind(target, -0.707f);
		}

        public bool IsAhead(Vector3 target, float cosThreshold)
		{
			Vector3 targetDirection = (target - this.Position);
            targetDirection.Normalize();
            return Vector3.Dot(this.Forward, targetDirection) > cosThreshold;
		}
        public bool IsAside(Vector3 target, float cosThreshold)
		{
			Vector3 targetDirection = (target - this.Position);
            targetDirection.Normalize();
            float dp = Vector3.Dot(this.Forward, targetDirection);
			return (dp < cosThreshold) && (dp > -cosThreshold);
		}
        public bool IsBehind(Vector3 target, float cosThreshold)
		{
			Vector3 targetDirection = (target - this.Position);
            targetDirection.Normalize();
            return Vector3.Dot(this.Forward, targetDirection) < cosThreshold;
		}

		// xxx cwr 9-6-02 temporary to support old code
		protected struct PathIntersection
		{
			public bool intersect;
			public float distance;
            public Vector3 surfacePoint;
            public Vector3 surfaceNormal;
			public SphericalObstacle obstacle;
		}

		// xxx experiment cwr 9-6-02
		protected void FindNextIntersectionWithSphere(SphericalObstacle obs, ref PathIntersection intersection)
		{
			// This routine is based on the Paul Bourke's derivation in:
			//   Intersection of a Line and a Sphere (or circle)
			//   http://www.swin.edu.au/astronomy/pbourke/geometry/sphereline/

			float b, c, d, p, q, s;
			Vector3 lc;

			// initialize pathIntersection object
			intersection.intersect = false;
			intersection.obstacle = obs;

			// find "local center" (lc) of sphere in boid's coordinate space
			lc = this.LocalizePosition(obs.Center);

			// computer line-sphere intersection parameters
			b = -2 * lc.z;
			c = Utilities.Square(lc.x) + Utilities.Square(lc.y) + Utilities.Square(lc.z) -
				Utilities.Square(obs.Radius + this.Radius);
			d = (b * b) - (4 * c);

			// when the path does not intersect the sphere
			if (d < 0) return;

			// otherwise, the path intersects the sphere in two Points with
			// parametric coordinates of "p" and "q".
			// (If "d" is zero the two Points are coincident, the path is tangent)
			s = (float)Math.Sqrt(d);
			p = (-b + s) / 2;
			q = (-b - s) / 2;

			// both intersections are behind us, so no potential collisions
			if ((p < 0) && (q < 0)) return;

			// at least one intersection is in front of us
			intersection.intersect = true;
			intersection.distance =
				((p > 0) && (q > 0)) ?
				// both intersections are in front of us, find nearest one
				((p < q) ? p : q) :
				// otherwise only one intersections is in front, select it
				((p > 0) ? p : q);
		}
	}
}
