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
	public abstract class AbstractVehicle : LocalSpace, IVehicle
	{
		public abstract float Mass { get; set; }
		public abstract float Radius { get; set; }
        public abstract Vector3 Velocity { get; }
		public abstract Vector3 Acceleration { get; }
		public abstract float Speed { get; set; }

        public abstract Vector3 PredictFuturePosition(float predictionTime);

		public abstract float MaxForce { get; set; }
		public abstract float MaxSpeed { get; set; }
	}
}
