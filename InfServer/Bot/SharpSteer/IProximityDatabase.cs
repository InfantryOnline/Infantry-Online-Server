// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
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
	public interface IProximityDatabase<ContentType>
	{
		// allocate a token to represent a given client object in this database
		ITokenForProximityDatabase<ContentType> AllocateToken(ContentType parentObject);

		// returns the number of tokens in the proximity database
		int Count { get; }
	}
}
