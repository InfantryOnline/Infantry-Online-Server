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
	public interface IVehicle : ILocalSpace
	{
        // mass (defaults to unity so acceleration=force)
		float Mass { get; set; }

        // size of bounding sphere, for obstacle avoidance, etc.
		float Radius { get; set; }

        // velocity of vehicle
        Vector3 Velocity { get; }

		/// <summary>
		/// Gets the acceleration of the vehicle.
		/// </summary>
		Vector3 Acceleration { get; }
		
		// speed of vehicle (may be faster than taking magnitude of velocity)
		float Speed { get; set; }

        // predict position of this vehicle at some time in the future
        //(assumes velocity remains constant)
        Vector3 PredictFuturePosition(float predictionTime);

        // ----------------------------------------------------------------------
        // XXX this vehicle-model-specific functionality functionality seems out
        // XXX of place on the abstract base class, but for now it is expedient

        // the maximum steering force this vehicle can apply
		float MaxForce { get; set; }

        // the maximum speed this vehicle is allowed to move
		float MaxSpeed { get; set; }
	}
}
