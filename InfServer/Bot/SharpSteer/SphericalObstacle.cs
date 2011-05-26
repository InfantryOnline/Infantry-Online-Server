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
	/// <summary>
	/// SphericalObstacle a simple concrete type of obstacle.
	/// </summary>
	public class SphericalObstacle : IObstacle
	{
		SeenFromState seenFrom;

		public float Radius;
        public Vector3 Center;

		// constructors
		public SphericalObstacle()
			: this(1, Vector3.Zero)
		{
		}
        public SphericalObstacle(float r, Vector3 c)
		{
			Radius = r;
			Center = c;
		}

		public SeenFromState SeenFrom
		{
			get { return seenFrom; }
			set { seenFrom = value; }
		}

		// XXX 4-23-03: Temporary work around (see comment above)
		//
		// Checks for intersection of the given spherical obstacle with a
		// volume of "likely future vehicle positions": a cylinder along the
		// current path, extending minTimeToCollision seconds along the
		// forward axis from current position.
		//
		// If they intersect, a collision is imminent and this function returns
		// a steering force pointing laterally away from the obstacle's center.
		//
		// Returns a zero vector if the obstacle is outside the cylinder
		//
		// xxx couldn't this be made more compact using localizePosition?

        public Vector3 SteerToAvoid(IVehicle v, float minTimeToCollision)
		{
			// minimum distance to obstacle before avoidance is required
			float minDistanceToCollision = minTimeToCollision * v.Speed;
			float minDistanceToCenter = minDistanceToCollision + Radius;

			// contact distance: sum of radii of obstacle and vehicle
			float totalRadius = Radius + v.Radius;

			// obstacle center relative to vehicle position
			Vector3 localOffset = Center - v.Position;

			// distance along vehicle's forward axis to obstacle's center
            float forwardComponent = Vector3.Dot(localOffset, v.Forward);
			Vector3 forwardOffset = v.Forward * forwardComponent;

			// offset from forward axis to obstacle's center
			Vector3 offForwardOffset = localOffset - forwardOffset;

			// test to see if sphere overlaps with obstacle-free corridor
			bool inCylinder = offForwardOffset.LengthSquared < totalRadius;
			bool nearby = forwardComponent < minDistanceToCenter;
			bool inFront = forwardComponent > 0;

			// if all three conditions are met, steer away from sphere center
			if (inCylinder && nearby && inFront)
			{
				return offForwardOffset * -1;
			}
			else
			{
                return Vector3.Zero;
			}
		}
	}
}
