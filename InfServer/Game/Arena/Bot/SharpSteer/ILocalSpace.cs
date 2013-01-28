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
	/// A local coordinate system for 3d space
	/// <para>
	/// Provides functionality such as transforming from local space to global
	/// space and vice versa.  Also regenerates a valid space from a perturbed
	/// "forward vector" which is the basis of abnstract vehicle turning.
	/// </para>
	/// <para>
	/// These are comparable to a 4x4 homogeneous transformation matrix where the
	/// 3x3 (R) portion is constrained to be a pure rotation (no shear or scale).
	/// The rows of the 3x3 R matrix are the basis vectors of the space.  They are
	/// all constrained to be mutually perpendicular and of unit length.  The top
	/// ("x") row is called "side", the middle ("y") row is called "up" and the
	/// bottom ("z") row is called forward.  The translation vector is called
	/// "position".  Finally the "homogeneous column" is always [0 0 0 1].
	/// </para>
	/// <code>
	/// [ R R R  0 ]      [ Sx Sy Sz  0 ]
	/// [ R R R  0 ]      [ Ux Uy Uz  0 ]
	/// [ R R R  0 ]  ->  [ Fx Fy Fz  0 ]
	/// [          ]      [             ]
	/// [ T T T  1 ]      [ Tx Ty Tz  1 ]
	/// </code>
	/// </summary>
	public interface ILocalSpace
	{
		/// <summary>
		/// Gets or sets the side.
		/// </summary>
		Vector3 Side { get; set; }
		/// <summary>
		/// Gets or sets the up.
		/// </summary>
		Vector3 Up { get; set; }
		/// <summary>
		/// Gets or sets the forward.
		/// </summary>
        Vector3 Forward { get; set; }
		/// <summary>
		/// Gets or sets the position.
		/// </summary>
		Vector3 Position { get; set; }

		/// <summary>
		/// Indicates whether the local space is right handed.
		/// </summary>
		bool IsRightHanded { get; }

		/// <summary>
		/// Resets the transform to identity.
		/// </summary>
        void ResetLocalSpace();

		/// <summary>
		/// Transforms a direction in global space to its equivalent in local space.
		/// </summary>
		/// <param name="globalDirection">The global space direction to transform.</param>
		/// <returns>The global space direction transformed to local space .</returns>
        Vector3 LocalizeDirection(Vector3 globalDirection);

		/// <summary>
		/// Transforms a point in global space to its equivalent in local space.
		/// </summary>
		/// <param name="globalPosition">The global space position to transform.</param>
		/// <returns>The global space position transformed to local space.</returns>
        Vector3 LocalizePosition(Vector3 globalPosition);

		// t
		/// <summary>
		/// Transforms a direction in local space to its equivalent in global space.
		/// </summary>
		/// <param name="localDirection">The local space direction to tranform.</param>
		/// <returns>The local space direction transformed to global space</returns>
		Vector3 GlobalizeDirection(Vector3 localDirection);

		/// <summary>
		/// Transforms a point in local space to its equivalent in global space.
		/// </summary>
		/// <param name="localPosition">The local space position to tranform.</param>
		/// <returns>The local space position transformed to global space.</returns>
        Vector3 GlobalizePosition(Vector3 localPosition);

		/// <summary>
		/// Sets the "side" basis vector to normalized cross product of forward and up.
		/// </summary>
        void SetUnitSideFromForwardAndUp();

		/// <summary>
		/// Regenerates the orthonormal basis vectors given a new forward.
		/// </summary>
		/// <param name="newUnitForward">The new unit-length forward.</param>
        void RegenerateOrthonormalBasisUF(Vector3 newUnitForward);

		/// <summary>
		/// Regenerates the orthonormal basis vectors given a new forward.
		/// </summary>
		/// <param name="newForward">The new forward.</param>
		void RegenerateOrthonormalBasis(Vector3 newForward);

		/// <summary>
		/// Regenerates the orthonormal basis vectors given a new forward and up.
		/// </summary>
		/// <param name="newForward">The new forward.</param>
		/// <param name="newUp">The new up.</param>
		void RegenerateOrthonormalBasis(Vector3 newForward, Vector3 newUp);

		/// <summary>
		/// Rotates, in the canonical direction, a vector pointing in the
		/// "forward" (+Z) direction to the "side" (+/-X) direction as implied
		/// by IsRightHanded.
		/// </summary>
		/// <param name="value">The local space vector.</param>
		/// <returns>The rotated vector.</returns>
        Vector3 LocalRotateForwardToSide(Vector3 value);

		/// <summary>
		/// Rotates, in the canonical direction, a vector pointing in the
		/// "forward" (+Z) direction to the "side" (+/-X) direction as implied
		/// by IsRightHanded.
		/// </summary>
		/// <param name="value">The global space forward.</param>
		/// <returns>The rotated vector.</returns>
		Vector3 GlobalRotateForwardToSide(Vector3 value);
	}
}
