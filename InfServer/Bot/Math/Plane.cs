#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

The math library included in this project, in addition to being a derivative of
the works of Ogre, also include derivative work of the free portion of the 
Wild Magic mathematics source code that is distributed with the excellent
book Game Engine Design.
http://www.wild-magic.com/

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: Plane.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations



#endregion Namespace Declarations

using System.Runtime.InteropServices;

namespace Axiom.Math
{
	/// <summary>
	/// Defines a plane in 3D space.
	/// </summary>
	/// <remarks>
	/// A plane is defined in 3D space by the equation
	/// Ax + By + Cz + D = 0
	///
	/// This equates to a vector (the normal of the plane, whose x, y
	/// and z components equate to the coefficients A, B and C
	/// respectively), and a constant (D) which is the distance along
	/// the normal you have to go to move the plane back to the origin.
	/// </remarks>
	[StructLayout( LayoutKind.Sequential )]
	public struct Plane
	{
		#region Fields

		/// <summary>
		///		Distance from the origin.
		/// </summary>
		public Real D;

		/// <summary>
		///		Direction the plane is facing.
		/// </summary>
		public Vector3 Normal;

		private static readonly Plane nullPlane = new Plane( Vector3.Zero, 0 );
		public static Plane Null
		{
			get
			{
				return nullPlane;
			}
		}

		#endregion Fields

		#region Constructors

		//public Plane()
		//{
		//    this.Normal = Vector3.Zero;
		//    this.D = Real.NaN;
		//}

		public Plane( Plane plane )
		{
			this.Normal = plane.Normal;
			this.D = plane.D;
		}

		/// <summary>
		///		Construct a plane through a normal, and a distance to move the plane along the normal.
		/// </summary>
		/// <param name="normal"></param>
		/// <param name="constant"></param>
		public Plane( Vector3 normal, Real constant )
		{
			this.Normal = normal;
			this.D = -constant;
		}

		public Plane( Vector3 normal, Vector3 point )
		{
			this.Normal = normal;
			this.D = -normal.Dot( point );
		}

		/// <summary>
		///		Construct a plane from 3 coplanar points.
		/// </summary>
		/// <param name="point0">First point.</param>
		/// <param name="point1">Second point.</param>
		/// <param name="point2">Third point.</param>
		public Plane( Vector3 point0, Vector3 point1, Vector3 point2 )
		{
			Vector3 edge1 = point1 - point0;
			Vector3 edge2 = point2 - point0;
			this.Normal = edge1.Cross( edge2 );
			this.Normal.Normalize();
			this.D = -this.Normal.Dot( point0 );
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public PlaneSide GetSide( Vector3 point )
		{
			Real distance = this.GetDistance( point );

			if ( distance < 0.0f )
			{
				return PlaneSide.Negative;
			}

			if ( distance > 0.0f )
			{
				return PlaneSide.Positive;
			}

			return PlaneSide.None;
		}

		/// <summary>
		/// Returns the side where the aligneBox is. the flag Both indicates an intersecting box.
		/// one corner ON the plane is sufficient to consider the box and the plane intersecting.
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
		public PlaneSide GetSide( AxisAlignedBox box )
		{
			if ( box.IsNull )
			{
				return PlaneSide.None;
			}
			if ( box.IsInfinite )
			{
				return PlaneSide.Both;
			}

			return this.GetSide( box.Center, box.HalfSize );
		}

		/// <summary>
		///     Returns which side of the plane that the given box lies on.
		///     The box is defined as centre/half-size pairs for effectively.
		/// </summary>
		/// <param name="centre">The centre of the box.</param>
		/// <param name="halfSize">The half-size of the box.</param>
		/// <returns>
		///     Positive if the box complete lies on the "positive side" of the plane,
		///     Negative if the box complete lies on the "negative side" of the plane,
		///     and Both if the box intersects the plane.
		/// </returns>
		public PlaneSide GetSide( Vector3 centre, Vector3 halfSize )
		{
			// Calculate the distance between box centre and the plane
			Real dist = this.GetDistance( centre );

			// Calculate the maximise allows absolute distance for
			// the distance between box centre and plane
			Real maxAbsDist = this.Normal.AbsDot( halfSize );

			if ( dist < -maxAbsDist )
			{
				return PlaneSide.Negative;
			}

			if ( dist > +maxAbsDist )
			{
				return PlaneSide.Positive;
			}

			return PlaneSide.Both;
		}

		/// <summary>
		/// This is a pseudodistance. The sign of the return value is
		/// positive if the point is on the positive side of the plane,
		/// negative if the point is on the negative side, and zero if the
		///	 point is on the plane.
		/// The absolute value of the return value is the true distance only
		/// when the plane normal is a unit length vector.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public Real GetDistance( Vector3 point )
		{
			return this.Normal.Dot( point ) + this.D;
		}

		/// <summary>
		/// Redefine this plane based on a normal and a point.
		/// </summary>
		/// <param name="rkNormal">Normal vector</param>
		/// <param name="rkPoint">Point vector</param>
		public void Redefine( Vector3 rkNormal, Vector3 rkPoint )
		{
			this.Normal = rkNormal;
			this.D = -rkNormal.Dot( rkPoint );
		}

		/// <summary>
		///		Construct a plane from 3 coplanar points.
		/// </summary>
		/// <param name="point0">First point.</param>
		/// <param name="point1">Second point.</param>
		/// <param name="point2">Third point.</param>
		public void Redefine( Vector3 point0, Vector3 point1, Vector3 point2 )
		{
			Vector3 edge1 = point1 - point0;
			Vector3 edge2 = point2 - point0;
			this.Normal = edge1.Cross( edge2 );
			this.Normal.Normalize();
			this.D = -this.Normal.Dot( point0 );
		}

		/// <summary>
		///     Project a point onto the plane.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public Vector3 ProjectVector( Vector3 point )
		{
			// We know plane normal is unit length, so use simple method
			Matrix3 xform;

			xform.m00 = 1.0f - this.Normal.x * this.Normal.x;
			xform.m01 = -this.Normal.x * this.Normal.y;
			xform.m02 = -this.Normal.x * this.Normal.z;
			xform.m10 = -this.Normal.y * this.Normal.x;
			xform.m11 = 1.0f - this.Normal.y * this.Normal.y;
			xform.m12 = -this.Normal.y * this.Normal.z;
			xform.m20 = -this.Normal.z * this.Normal.x;
			xform.m21 = -this.Normal.z * this.Normal.y;
			xform.m22 = 1.0f - this.Normal.z * this.Normal.z;

			return xform * point;
		}

		#endregion Methods

		#region Object overrides

		/// <summary>
		///		Object method for testing equality.
		/// </summary>
		/// <param name="obj">Object to test.</param>
		/// <returns>True if the 2 planes are logically equal, false otherwise.</returns>
		public override bool Equals( object obj )
		{
			return obj is Plane && this == (Plane)obj;
		}

		/// <summary>
		///		Gets the hashcode for this Plane.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.D.GetHashCode() ^ this.Normal.GetHashCode();
		}

		/// <summary>
		///		Returns a string representation of this Plane.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format( "Distance: {0} Normal: {1}", this.D, this.Normal );
		}

		#endregion

		#region Operator Overloads

		/// <summary>
		///		Compares 2 Planes for equality.
		/// </summary>
		/// <param name="left">First plane.</param>
		/// <param name="right">Second plane.</param>
		/// <returns>true if equal, false if not equal.</returns>
		public static bool operator ==( Plane left, Plane right )
		{
			object l = left;
			object r = right;
			if ( l == null || r == null )
			{
				if ( l == null && r == null )
				{
					return true;
				}

				return false;
			}

			return ( left.D == right.D ) && ( left.Normal == right.Normal );
		}

		/// <summary>
		///		Compares 2 Planes for inequality.
		/// </summary>
		/// <param name="left">First plane.</param>
		/// <param name="right">Second plane.</param>
		/// <returns>true if not equal, false if equal.</returns>
		public static bool operator !=( Plane left, Plane right )
		{
			object l = left;
			object r = right;
			if ( l == null || r == null )
			{
				if ( l == null && r == null )
				{
					return false;
				}

				return true;
			}

			return ( left.D != right.D ) || ( left.Normal != right.Normal );
		}

		#endregion
	}
}