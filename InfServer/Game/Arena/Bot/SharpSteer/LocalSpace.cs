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

using Axiom.Math;

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// LocalSpaceMixin is a mixin layer, a class template with a paramterized base
	/// class.  Allows "LocalSpace-ness" to be layered on any class.
	/// </summary>
	public class LocalSpace : ILocalSpace
	{
		// transformation as three orthonormal unit basis vectors and the
		// origin of the local space.  These correspond to the "rows" of
		// a 3x4 transformation matrix with [0 0 0 1] as the final column

        Vector3 side;     //    side-pointing unit basis vector
        Vector3 up;       //  upward-pointing unit basis vector
        Vector3 forward;  // forward-pointing unit basis vector
        Vector3 position; // origin of local space

		// accessors (get and set) for side, up, forward and position
        public Vector3 Side
		{
			get { return side; }
			set { side = value; }
		}
        public Vector3 Up
		{
			get { return up; }
			set { up = value; }
		}
        public Vector3 Forward
		{
			get { return forward; }
			set { forward = value; }
		}
        public virtual Vector3 Position
		{
			get { return position; }
			set { position = value; }
		}

        public Vector3 SetUp(float x, float y, float z)
		{
            up.x = x;
            up.y = y;
            up.z = z;

			return up;
		}
        public Vector3 SetForward(float x, float y, float z)
		{
            forward.x = x;
            forward.y = y;
            forward.z = z;

			return forward;
		}
        public Vector3 SetPosition(float x, float y, float z)
		{
            position.x = x;
            position.y = y;
            position.z = z;

			return position;
		}

		// ------------------------------------------------------------------------
		// Global compile-time switch to control handedness/chirality: should
		// LocalSpace use a left- or right-handed coordinate system?  This can be
		// overloaded in derived types (e.g. vehicles) to change handedness.
		public bool IsRightHanded { get { return true; } }

		// ------------------------------------------------------------------------
		// constructors
		public LocalSpace()
		{
			ResetLocalSpace();
		}

        public LocalSpace(Vector3 Side, Vector3 Up, Vector3 Forward, Vector3 Position)
		{
			side = Side;
			up = Up;
			forward = Forward;
			position = Position;
		}

        public LocalSpace(Vector3 Up, Vector3 Forward, Vector3 Position)
		{
			up = Up;
			forward = Forward;
			position = Position;
			SetUnitSideFromForwardAndUp();
		}

		// ------------------------------------------------------------------------
		// reset transform: set local space to its identity state, equivalent to a
		// 4x4 homogeneous transform like this:
		//
		//     [ X 0 0 0 ]
		//     [ 0 1 0 0 ]
		//     [ 0 0 1 0 ]
		//     [ 0 0 0 1 ]
		//
		// where X is 1 for a left-handed system and -1 for a right-handed system.
		public void ResetLocalSpace()
		{
			forward = Vector3.UnitZ;
			side = LocalRotateForwardToSide(Forward);
			up = Vector3.UnitY;
			position = Vector3.Zero;
		}

		// ------------------------------------------------------------------------
		// transform a direction in global space to its equivalent in local space
        public Vector3 LocalizeDirection(Vector3 globalDirection)
        {
			// dot offset with local basis vectors to obtain local coordiantes
            return new Vector3(Vector3.Dot(globalDirection, side), Vector3.Dot(globalDirection, up), Vector3.Dot(globalDirection, forward));
		}

		// ------------------------------------------------------------------------
		// transform a point in global space to its equivalent in local space
        public Vector3 LocalizePosition(Vector3 globalPosition)
		{
			// global offset from local origin
            Vector3 globalOffset = globalPosition - position;

			// dot offset with local basis vectors to obtain local coordiantes
			return LocalizeDirection(globalOffset);
		}

		// ------------------------------------------------------------------------
		// transform a point in local space to its equivalent in global space
        public Vector3 GlobalizePosition(Vector3 localPosition)
		{
			return position + GlobalizeDirection(localPosition);
		}

		// ------------------------------------------------------------------------
		// transform a direction in local space to its equivalent in global space
        public Vector3 GlobalizeDirection(Vector3 localDirection)
		{
			return ((side * localDirection.x) +
					(up * localDirection.y) +
					(forward * localDirection.z));
		}

		// ------------------------------------------------------------------------
		// set "side" basis vector to normalized cross product of forward and up
		public void SetUnitSideFromForwardAndUp()
		{
			// derive new unit side basis vector from forward and up
			if (IsRightHanded)
				side = Vector3.Cross(forward, up);
			else
                side = Vector3.Cross(up, forward);
			
            side.Normalize();
		}

		// ------------------------------------------------------------------------
		// regenerate the orthonormal basis vectors given a new forward
		//(which is expected to have unit length)
        public void RegenerateOrthonormalBasisUF(Vector3 newUnitForward)
		{
			forward = newUnitForward;

			// derive new side basis vector from NEW forward and OLD up
			SetUnitSideFromForwardAndUp();

			// derive new Up basis vector from new Side and new Forward
			//(should have unit length since Side and Forward are
			// perpendicular and unit length)
			if (IsRightHanded)
                up = Vector3.Cross(side, forward);
			else
                up = Vector3.Cross(forward, side);
		}

		// for when the new forward is NOT know to have unit length
        public void RegenerateOrthonormalBasis(Vector3 newForward)
		{
            newForward.Normalize();

			RegenerateOrthonormalBasisUF(newForward);
		}

		// for supplying both a new forward and and new up
        public void RegenerateOrthonormalBasis(Vector3 newForward, Vector3 newUp)
		{
			up = newUp;
            newForward.Normalize();
			RegenerateOrthonormalBasis(newForward);
		}

		// ------------------------------------------------------------------------
		// rotate, in the canonical direction, a vector pointing in the
		// "forward"(+Z) direction to the "side"(+/-X) direction
        public Vector3 LocalRotateForwardToSide(Vector3 value)
		{
			return new Vector3(IsRightHanded ? (float)(-value.z) : (float)(+value.z), value.y, value.x);
		}

		// not currently used, just added for completeness
        public Vector3 GlobalRotateForwardToSide(Vector3 value)
		{
            Vector3 localForward = LocalizeDirection(value);
            Vector3 localSide = LocalRotateForwardToSide(localForward);
			return GlobalizeDirection(localSide);
		}
	}
}
