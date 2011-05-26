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
	/// Pathway: a pure virtual base class for an abstract pathway in space, as for
	/// example would be used in path following.
	/// </summary>
	public abstract class Pathway
	{
		// Given an arbitrary point ("A"), returns the nearest point ("P") on
		// this path.  Also returns, via output arguments, the path tangent at
		// P and a measure of how far A is outside the Pathway's "tube".  Note
		// that a negative distance indicates A is inside the Pathway.
        public abstract Vector3 MapPointToPath(Vector3 point, out Vector3 tangent, out float outside);

		// given a distance along the path, convert it to a point on the path
        public abstract Vector3 MapPathDistanceToPoint(float pathDistance);

		// Given an arbitrary point, convert it to a distance along the path.
        public abstract float MapPointToPathDistance(Vector3 point);

		// is the given point inside the path tube?
        public bool IsInsidePath(Vector3 point)
		{
			float outside;
			Vector3 tangent;
			MapPointToPath(point, out tangent, out outside);
			return outside < 0;
		}

		// how far outside path tube is the given point?  (negative is inside)
        public float HowFarOutsidePath(Vector3 point)
		{
			float outside;
			Vector3 tangent;
			MapPointToPath(point, out tangent, out outside);
			return outside;
		}
	}
}
