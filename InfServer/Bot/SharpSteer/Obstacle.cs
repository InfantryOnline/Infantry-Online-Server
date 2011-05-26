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
	public enum SeenFromState
	{
		Outside,
		Inside,
		Both
	}

	/// <summary>
	/// Obstacle: a pure virtual base class for an abstract shape in space, to be
	/// used with obstacle avoidance.
	/// 
	/// XXX this should define generic methods for querying the obstacle shape
	/// </summary>
	public interface IObstacle
	{
		SeenFromState SeenFrom { get; set; }

		// XXX 4-23-03: Temporary work around (see comment above)
        Vector3 SteerToAvoid(IVehicle v, float minTimeToCollision);
	}
}
