#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

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
//     <id value="$Id: Utility.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Math.Collections;

//
//using Radian = System.Single;
//using Degree = System.Single;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Math
{
	public sealed class Utility
	{
		public static readonly Real PI = (Real)System.Math.PI;
		public static readonly Real TWO_PI = PI * 2.0f;
		public static readonly Real HALF_PI = PI * 0.5f;

		public static readonly Real RADIANS_PER_DEGREE = PI / 180.0f;
		public static readonly Real DEGREES_PER_RADIAN = 180.0f / PI;

		private static Random random = new Random();

		/// <summary>
		/// Empty static constructor
		/// DO NOT DELETE.  It needs to be here because:
		/// 
		///     # The presence of a static constructor suppresses beforeFieldInit.
		///     # Static field variables are initialized before the static constructor is called.
		///     # Having a static constructor is the only way to ensure that all resources are 
		///       initialized before other static functions are called.
		/// 
		/// (from "Static Constructors Demystified" by Satya Komatineni
		///  http://www.ondotnet.com/pub/a/dotnet/2003/07/07/staticxtor.html)
		/// </summary>
		static Utility()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		private Utility()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aabb"></param>
		/// <returns></returns>
		public static Real BoundingRadiusFromAABB( AxisAlignedBox aabb )
		{
			Vector3 max = aabb.Maximum;
			Vector3 min = aabb.Minimum;

			Vector3 magnitude = max;
			magnitude.Ceil( -max );
			magnitude.Ceil( min );
			magnitude.Ceil( -min );

			return magnitude.Length;
		}

		/// <summary>
		///		Converts radians to degrees.
		/// </summary>
		/// <param name="radians"></param>
		/// <returns></returns>
		public static Degree RadiansToDegrees( Radian radians )
		{
			return radians;
		}

		/// <summary>
		///		Converts degrees to radians.
		/// </summary>
		/// <param name="degrees"></param>
		/// <returns></returns>
		public static Real DegreesToRadians( Real degrees )
		{
			return degrees * RADIANS_PER_DEGREE;
		}

		/// <summary>
		///		Converts radians to degrees.
		/// </summary>
		/// <param name="radians"></param>
		/// <returns></returns>
		public static Real RadiansToDegrees( Real radians )
		{
			return radians * DEGREES_PER_RADIAN;
		}

		/// <summary>
		///     Compares float values for equality, taking into consideration
		///     that floating point values should never be directly compared using
		///     the '==' operator.  2 floats could be conceptually equal, but vary by a 
		///     float.Epsilon which would fail in a direct comparison.  To circumvent that,
		///     a tolerance value is used to see if the difference between the 2 floats
		///     is less than the desired amount of accuracy.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool RealEqual( Real a, Real b, Real tolerance )
		{
			return ( System.Math.Abs( b - a ) <= tolerance );
		}

		/// <summary>
		///     Compares float values for equality, taking into consideration
		///     that floating point values should never be directly compared using
		///     the '==' operator.  2 floats could be conceptually equal, but vary by a 
		///     float.Epsilon which would fail in a direct comparison.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool RealEqual( Real a, Real b )
		{
			return ( System.Math.Abs( b - a ) <= Real.Epsilon );
		}

		public static Real ParseReal( string value )
		{
			return Real.Parse( value, new System.Globalization.CultureInfo( "en-US" ) );
		}

		/// <summary>
		///     Returns the sign of a real number.
		/// The result will be -1 for a negative number, 0 for zero and 1 for positive number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static int Sign( Real number )
		{
			return System.Math.Sign( number );
		}

		/// <summary>
		///	Returns the sine of the specified angle.
		/// </summary>
		public static Real Sin( Radian angle )
		{
			return (Real)System.Math.Sin( (double)angle );
		}

		/// <summary>
		///	Returns the angle whose cosine is the specified number.
		/// </summary>
		public static Radian ASin( Real angle )
		{
			return (Radian)System.Math.Asin( angle );
		}

		/// <summary>
		///	Returns the cosine of the specified angle.
		/// </summary>
		public static Real Cos( Radian angle )
		{
			return (Real)System.Math.Cos( (double)angle );
		}

		/// <summary>
		///	Returns the angle whose cosine is the specified number.
		/// </summary>
		public static Radian ACos( Real angle )
		{

			// HACK: Ok, this needs to be looked at.  The decimal precision of float values can sometimes be 
			// *slightly* off from what is loaded from .skeleton files.  In some scenarios when we end up having 
			// a cos value calculated above that is just over 1 (i.e. 1.000000012), which the ACos of is Nan, thus 
			// completly throwing off node transformations and rotations associated with an animation.
			if ( angle > 1 )
			{
				angle = 1.0f;
			}

			return (Radian)System.Math.Acos( angle );
		}

		/// <summary>
		/// Returns the tangent of the specified angle.
		/// </summary>
		public static Real Tan( Radian value )
		{
			return (Real)System.Math.Tan( (double)value );
		}

		/// <summary>
		/// Return the angle whos tangent is the specified number.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Radian ATan( Real value )
		{
			return (Radian)System.Math.Atan( value );
		}

		/// <summary>
		/// Returns the angle whose tangent is the quotient of the two specified numbers.
		/// </summary>
		public static Radian ATan( Real y, Real x )
		{
			return (Radian)System.Math.Atan2( y, x );
		}

		public static Real ATan2( Real y, Real x )
		{
			return System.Math.Atan2( y, x );
		}

		/// <summary>
		///		Returns the square root of a number.
		/// </summary>
		/// <remarks>This is one of the more expensive math operations.  Avoid when possible.</remarks>
		/// <param name="number"></param>
		/// <returns></returns>
		public static Real Sqrt( Real number )
		{
			return (Real)System.Math.Sqrt( number );
		}

		/// <summary>
		///    Inverse square root.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static Real InvSqrt( Real number )
		{
			return 1 / Sqrt( number );
		}

		/// <summary>
		///     Raise a number to a power.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>The number x raised to power y</returns>
		public static Real Pow( Real x, Real y )
		{
			return (Real)System.Math.Pow( (double)x, (double)y );
		}

		/// <summary>
		///		Returns the absolute value of the supplied number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static Real Abs( Real number )
		{
			return (Real)System.Math.Abs( number );
		}

		/// <summary>
		/// Returns the maximum of the two supplied values.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static Real Max( Real lhs, Real rhs )
		{
			return lhs > rhs ? lhs : rhs;
		}

		/// <summary>
		/// Returns the maximum of the two supplied values.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static long Max( long lhs, long rhs )
		{
			return lhs > rhs ? lhs : rhs;
		}

		/// <summary>
		///     Finds the first maximum value in the array and returns the index of it.
		/// </summary>
		/// <param name="values">Array of values containing one value at least.</param>
		/// <returns></returns>
		public static int Max( Real[] values )
		{
			Debug.Assert( values != null && values.Length > 0 );

			int maxIndex = 0;
			Real max = values[ 0 ];
			for ( int i = 1; i < values.Length; i++ )
				if ( values[ i ] > max )
				{
					max = values[ i ];
					maxIndex = i;
				}

			return maxIndex;
		}

		/// <summary>
		/// Returns the minumum of the two supplied values.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static Real Min( Real lhs, Real rhs )
		{
			return lhs < rhs ? lhs : rhs;
		}

		/// <summary>
		/// Returns the minumum of the two supplied values.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static long Min( long lhs, long rhs )
		{
			return lhs < rhs ? lhs : rhs;
		}

		/// <summary>
		///     Finds the first minimum value in the array and returns the index of it.
		/// </summary>
		/// <param name="values">Array of values containing one value at least.</param>
		/// <returns></returns>
		public static int Min( Real[] values )
		{
			Debug.Assert( values != null && values.Length > 0 );

			int minIndex = 0;
			Real min = values[ 0 ];
			for ( int i = 1; i < values.Length; i++ )
				if ( values[ i ] < min )
				{
					min = values[ i ];
					minIndex = i;
				}

			return minIndex;
		}

		/// <summary>
		/// Returns the smallest integer greater than or equal to the specified value.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static Real Ceiling( Real number )
		{
			return (Real)System.Math.Ceiling( number );
		}

		/// <summary>
		///    Returns a random value between the specified min and max values.
		/// </summary>
		/// <param name="min">Minimum value.</param>
		/// <param name="max">Maximum value.</param>
		/// <returns>A random value in the range [min,max].</returns>
		public static Real RangeRandom( Real min, Real max )
		{
			return ( max - min ) * UnitRandom() + min;
		}

		/// <summary>
		///    
		/// </summary>
		/// <returns></returns>
		public static Real UnitRandom()
		{
			return (Real)random.Next( Int32.MaxValue ) / (Real)Int32.MaxValue;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static Real SymmetricRandom()
		{
			return (Real)( 2.0f * UnitRandom() - 1.0f );
		}

		/// <summary>
		///     Builds a reflection matrix for the specified plane.
		/// </summary>
		/// <param name="plane"></param>
		/// <returns></returns>
		public static Matrix4 BuildReflectionMatrix( Plane plane )
		{
			Vector3 normal = plane.Normal;

			return new Matrix4(
				-2.0f * normal.x * normal.x + 1.0f, -2.0f * normal.x * normal.y, -2.0f * normal.x * normal.z, -2.0f * normal.x * plane.D,
				-2.0f * normal.y * normal.x, -2.0f * normal.y * normal.y + 1.0f, -2.0f * normal.y * normal.z, -2.0f * normal.y * plane.D,
				-2.0f * normal.z * normal.x, -2.0f * normal.z * normal.y, -2.0f * normal.z * normal.z + 1.0f, -2.0f * normal.z * plane.D,
				0.0f, 0.0f, 0.0f, 1.0f );
		}

		/// <summary>
		///		Calculate a face normal, including the w component which is the offset from the origin.
		/// </summary>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		/// <returns></returns>
		public static Vector4 CalculateFaceNormal( Vector3 v1, Vector3 v2, Vector3 v3 )
		{
			Vector3 normal = CalculateBasicFaceNormal( v1, v2, v3 );

			// Now set up the w (distance of tri from origin
			return new Vector4( normal.x, normal.y, normal.z, -( normal.Dot( v1 ) ) );
		}

		/// <summary>
		///		Calculate a face normal, no w-information.
		/// </summary>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		/// <returns></returns>
		public static Vector3 CalculateBasicFaceNormal( Vector3 v1, Vector3 v2, Vector3 v3 )
		{
			Vector3 normal = ( v2 - v1 ).Cross( v3 - v1 );
			normal.Normalize();

			return normal;
		}
		/// <summary>
		///		Calculate a face normal, no w-information.
		/// </summary>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		/// <returns></returns>
		public static Vector3 CalculateBasicFaceNormalWithoutNormalize( Vector3 v1, Vector3 v2, Vector3 v3 )
		{
			Vector3 normal = ( v2 - v1 ).Cross( v3 - v1 );
			return normal;
		}
		/// <summary>
		///    Calculates the tangent space vector for a given set of positions / texture coords.
		/// </summary>
		/// <remarks>
		///    Adapted from bump mapping tutorials at:
		///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
		///    author : paul.baker@univ.ox.ac.uk
		/// </remarks>
		/// <param name="position1"></param>
		/// <param name="position2"></param>
		/// <param name="position3"></param>
		/// <param name="u1"></param>
		/// <param name="v1"></param>
		/// <param name="u2"></param>
		/// <param name="v2"></param>
		/// <param name="u3"></param>
		/// <param name="v3"></param>
		/// <returns></returns>
		public static Vector3 CalculateTangentSpaceVector(
			Vector3 position1, Vector3 position2, Vector3 position3, Real u1, Real v1, Real u2, Real v2, Real u3, Real v3 )
		{

			// side0 is the vector along one side of the triangle of vertices passed in, 
			// and side1 is the vector along another side. Taking the cross product of these returns the normal.
			Vector3 side0 = position1 - position2;
			Vector3 side1 = position3 - position1;
			// Calculate face normal
			Vector3 normal = side1.Cross( side0 );
			normal.Normalize();

			// Now we use a formula to calculate the tangent. 
			Real deltaV0 = v1 - v2;
			Real deltaV1 = v3 - v1;
			Vector3 tangent = deltaV1 * side0 - deltaV0 * side1;
			tangent.Normalize();

			// Calculate binormal
			Real deltaU0 = u1 - u2;
			Real deltaU1 = u3 - u1;
			Vector3 binormal = deltaU1 * side0 - deltaU0 * side1;
			binormal.Normalize();

			// Now, we take the cross product of the tangents to get a vector which 
			// should point in the same direction as our normal calculated above. 
			// If it points in the opposite direction (the dot product between the normals is less than zero), 
			// then we need to reverse the s and t tangents. 
			// This is because the triangle has been mirrored when going from tangent space to object space.
			// reverse tangents if necessary.
			Vector3 tangentCross = tangent.Cross( binormal );
			if ( tangentCross.Dot( normal ) < 0.0f )
			{
				tangent = -tangent;
				binormal = -binormal;
			}

			return tangent;
		}
		/// <summary>
		///		Checks wether a given point is inside a triangle, in a
		///		2-dimensional (Cartesian) space.
		/// </summary>
		/// <remarks>
		///		The vertices of the triangle must be given in either
		///		trigonometrical (anticlockwise) or inverse trigonometrical
		///		(clockwise) order.
		/// </remarks>
		/// <param name="px">
		///    The X-coordinate of the point.
		/// </param>
		/// <param name="py">
		///    The Y-coordinate of the point.
		/// </param>
		/// <param name="ax">
		///    The X-coordinate of the triangle's first vertex.
		/// </param>
		/// <param name="ay">
		///    The Y-coordinate of the triangle's first vertex.
		/// </param>
		/// <param name="bx">
		///    The X-coordinate of the triangle's second vertex.
		/// </param>
		/// <param name="by">
		///    The Y-coordinate of the triangle's second vertex.
		/// </param>
		/// <param name="cx">
		///    The X-coordinate of the triangle's third vertex.
		/// </param>
		/// <param name="cy">
		///    The Y-coordinate of the triangle's third vertex.
		/// </param>
		/// <returns>
		///    <list type="bullet">
		///        <item>
		///            <description><b>true</b> - the point resides in the triangle.</description>
		///        </item>
		///        <item>
		///            <description><b>false</b> - the point is outside the triangle</description>
		///         </item>
		///     </list>
		/// </returns>
		public static bool PointInTri2D( Real px, Real py, Real ax, Real ay, Real bx, Real by, Real cx, Real cy )
		{
			Real v1x, v2x, v1y, v2y;
			bool bClockwise;

			v1x = bx - ax;
			v1y = by - ay;

			v2x = px - bx;
			v2y = py - by;

			bClockwise = ( v1x * v2y - v1y * v2x >= 0.0 );

			v1x = cx - bx;
			v1y = cy - by;

			v2x = px - cx;
			v2y = py - cy;

			if ( ( v1x * v2y - v1y * v2x >= 0.0 ) != bClockwise )
				return false;

			v1x = ax - cx;
			v1y = ay - cy;

			v2x = px - ax;
			v2y = py - ay;

			if ( ( v1x * v2y - v1y * v2x >= 0.0 ) != bClockwise )
				return false;

			return true;
		}

		/// <summary>
		///    Method delegate with a simple signature. 
		///    Used to measure execution time of a method for instance.
		/// </summary>
		public delegate void SimpleMethodDelegate();

		/// <summary>
		///     Measure the execution time of a method.
		/// </summary>
		/// <param name="method"></param>
		/// <returns>The elapsed time in seconds.</returns>
		public static Real Measure( SimpleMethodDelegate method )
		{
			long start = System.Diagnostics.Stopwatch.GetTimestamp();

			method();

			double elapsed = (double)( System.Diagnostics.Stopwatch.GetTimestamp() - start );
			double freq = (double)System.Diagnostics.Stopwatch.Frequency;

			return (Real)( elapsed / freq );
		}


		#region Intersection Methods

		/// <summary>
		///    Tests an intersection between a ray and a box.
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="box"></param>
		/// <returns>A Pair object containing whether the intersection occurred, and the distance between the 2 objects.</returns>
		public static IntersectResult Intersects( Ray ray, AxisAlignedBox box )
		{
			Contract.RequiresNotNull( ray, "ray" );
			Contract.RequiresNotNull( box, "box" );

			if ( box.IsNull )
			{
				return new IntersectResult( false, 0 );
			}

			if ( box.IsInfinite )
			{
				return new IntersectResult( true, 0 );
			}

			Real lowt = 0.0f;
			Real t;
			bool hit = false;
			Vector3 hitPoint;
			Vector3 min = box.Minimum;
			Vector3 max = box.Maximum;

			// check origin inside first
			if ( ray.origin > min && ray.origin < max )
			{
				return new IntersectResult( true, 0.0f );
			}

			// check each face in turn, only check closest 3

			// Min X
			if ( ray.origin.x <= min.x && ray.direction.x > 0 )
			{
				t = ( min.x - ray.origin.x ) / ray.direction.x;

				if ( t >= 0 )
				{
					// substitue t back into ray and check bounds and distance
					hitPoint = ray.origin + ray.direction * t;

					if ( hitPoint.y >= min.y && hitPoint.y <= max.y &&
						hitPoint.z >= min.z && hitPoint.z <= max.z &&
						( !hit || t < lowt ) )
					{

						hit = true;
						lowt = t;
					}
				}
			}

			// Max X
			if ( ray.origin.x >= max.x && ray.direction.x < 0 )
			{
				t = ( max.x - ray.origin.x ) / ray.direction.x;

				if ( t >= 0 )
				{
					// substitue t back into ray and check bounds and distance
					hitPoint = ray.origin + ray.direction * t;

					if ( hitPoint.y >= min.y && hitPoint.y <= max.y &&
						hitPoint.z >= min.z && hitPoint.z <= max.z &&
						( !hit || t < lowt ) )
					{

						hit = true;
						lowt = t;
					}
				}
			}

			// Min Y
			if ( ray.origin.y <= min.y && ray.direction.y > 0 )
			{
				t = ( min.y - ray.origin.y ) / ray.direction.y;

				if ( t >= 0 )
				{
					// substitue t back into ray and check bounds and distance
					hitPoint = ray.origin + ray.direction * t;

					if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
						hitPoint.z >= min.z && hitPoint.z <= max.z &&
						( !hit || t < lowt ) )
					{

						hit = true;
						lowt = t;
					}
				}
			}

			// Max Y
			if ( ray.origin.y >= max.y && ray.direction.y < 0 )
			{
				t = ( max.y - ray.origin.y ) / ray.direction.y;

				if ( t >= 0 )
				{
					// substitue t back into ray and check bounds and distance
					hitPoint = ray.origin + ray.direction * t;

					if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
						hitPoint.z >= min.z && hitPoint.z <= max.z &&
						( !hit || t < lowt ) )
					{

						hit = true;
						lowt = t;
					}
				}
			}

			// Min Z
			if ( ray.origin.z <= min.z && ray.direction.z > 0 )
			{
				t = ( min.z - ray.origin.z ) / ray.direction.z;

				if ( t >= 0 )
				{
					// substitue t back into ray and check bounds and distance
					hitPoint = ray.origin + ray.direction * t;

					if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
						hitPoint.y >= min.y && hitPoint.y <= max.y &&
						( !hit || t < lowt ) )
					{

						hit = true;
						lowt = t;
					}
				}
			}

			// Max Z
			if ( ray.origin.z >= max.z && ray.direction.z < 0 )
			{
				t = ( max.z - ray.origin.z ) / ray.direction.z;

				if ( t >= 0 )
				{
					// substitue t back into ray and check bounds and distance
					hitPoint = ray.origin + ray.direction * t;

					if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
						hitPoint.y >= min.y && hitPoint.y <= max.y &&
						( !hit || t < lowt ) )
					{

						hit = true;
						lowt = t;
					}
				}
			}

			return new IntersectResult( hit, lowt );
		}

		public static IntersectResult Intersects( Ray ray, Vector3 a,
			Vector3 b, Vector3 c, Vector3 normal, bool positiveSide, bool negativeSide )
		{
			// Calculate intersection with plane.
			Real t;
			{
				Real denom = normal.Dot( ray.Direction );
				// Check intersect side
				if ( denom > +Real.Epsilon )
				{
					if ( !negativeSide )
						return new IntersectResult( false, 0 );
				}
				else if ( denom < -Real.Epsilon )
				{
					if ( !positiveSide )
						return new IntersectResult( false, 0 );
				}
				else
				{
					// Parallel or triangle area is close to zero when
					// the plane normal not Normalized.
					return new IntersectResult( false, 0 );
				}

				t = normal.Dot( a - ray.Origin ) / denom;
				if ( t < 0 )
				{
					return new IntersectResult( false, 0 );
				}
			}

			// Calculate the largest area projection plane in X, Y or Z.
			int i0, i1;
			{
				Real n0 = Math.Utility.Abs( normal[ 0 ] );
				Real n1 = Math.Utility.Abs( normal[ 1 ] );
				Real n2 = Math.Utility.Abs( normal[ 2 ] );

				i0 = 1;
				i1 = 2;
				if ( n1 > n2 )
				{
					if ( n1 > n0 )
						i0 = 0;
				}
				else
				{
					if ( n2 > n0 )
						i1 = 0;
				}

			}

			// Check the intersection point is inside the triangle.
			{
				Real u1 = b[ i0 ] - a[ i0 ];
				Real v1 = b[ i1 ] - a[ i1 ];
				Real u2 = c[ i0 ] - a[ i0 ];
				Real v2 = c[ i1 ] - a[ i1 ];
				Real u0 = t * ray.Direction[ i0 ] + ray.Origin[ i0 ] - a[ i0 ];
				Real v0 = t * ray.Direction[ i1 ] + ray.Origin[ i1 ] - a[ i1 ];

				Real alpha = u0 * v2 - u2 * v0;
				Real beta = u1 * v0 - u0 * v1;
				Real area = u1 * v2 - u2 * v1;

				// epsilon to avoid Real precision error
				Real EPSILON = 1e-3f;

				Real tolerance = -EPSILON * area;

				if ( area > 0 )
				{
					if ( alpha < tolerance || beta < tolerance || alpha + beta > area - tolerance )
						return new IntersectResult( false, 0 );
				}
				else
				{
					if ( alpha > tolerance || beta > tolerance || alpha + beta < area - tolerance )
						return new IntersectResult( false, 0 );
				}

			}

			return new IntersectResult( true, t );
		}

		public static IntersectResult Intersects( Ray ray, Vector3 a,
			Vector3 b, Vector3 c, bool positiveSide, bool negativeSide )
		{
			Vector3 normal = CalculateBasicFaceNormalWithoutNormalize( a, b, c );
			return Intersects( ray, a, b, c, normal, positiveSide, negativeSide );
		}

		/// <summary>
		///    Tests an intersection between two boxes.
		/// </summary>
		/// <param name="boxA">
		///    The primary box.
		/// </param>
		/// <param name="boxB">
		///    The box to test intersection with boxA.
		/// </param>
		/// <returns>
		///    <list type="bullet">
		///        <item>
		///            <description>None - There was no intersection between the 2 boxes.</description>
		///        </item>
		///        <item>
		///            <description>Contained - boxA is fully within boxB.</description>
		///         </item>
		///        <item>
		///            <description>Contains - boxB is fully within boxA.</description>
		///         </item>
		///        <item>
		///            <description>Partial - boxA is partially intersecting with boxB.</description>
		///         </item>
		///     </list>
		/// </returns>
		/// Submitted by: romout
		public static Intersection Intersects( AxisAlignedBox boxA, AxisAlignedBox boxB )
		{
			Contract.RequiresNotNull( boxA, "boxA" );
			Contract.RequiresNotNull( boxB, "boxB" );

			// grab the max and mix vectors for both boxes for comparison
			Vector3 minA = boxA.Minimum;
			Vector3 maxA = boxA.Maximum;
			Vector3 minB = boxB.Minimum;
			Vector3 maxB = boxB.Maximum;

			if ( ( minB.x < minA.x ) &&
				( maxB.x > maxA.x ) &&
				( minB.y < minA.y ) &&
				( maxB.y > maxA.y ) &&
				( minB.z < minA.z ) &&
				( maxB.z > maxA.z ) )
			{

				// boxA is within boxB
				return Intersection.Contained;
			}

			if ( ( minB.x > minA.x ) &&
				( maxB.x < maxA.x ) &&
				( minB.y > minA.y ) &&
				( maxB.y < maxA.y ) &&
				( minB.z > minA.z ) &&
				( maxB.z < maxA.z ) )
			{

				// boxB is within boxA
				return Intersection.Contains;
			}

			if ( ( minB.x > maxA.x ) ||
				( minB.y > maxA.y ) ||
				( minB.z > maxA.z ) ||
				( maxB.x < minA.x ) ||
				( maxB.y < minA.y ) ||
				( maxB.z < minA.z ) )
			{

				// not interesting at all
				return Intersection.None;
			}

			// if we got this far, they are partially intersecting
			return Intersection.Partial;
		}


		public static IntersectResult Intersects( Ray ray, Sphere sphere )
		{
			return Intersects( ray, sphere, false );
		}

		/// <summary>
		///		Ray/Sphere intersection test.
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="sphere"></param>
		/// <param name="discardInside"></param>
		/// <returns>Struct that contains a bool (hit?) and distance.</returns>
		public static IntersectResult Intersects( Ray ray, Sphere sphere, bool discardInside )
		{
			Contract.RequiresNotNull( ray, "ray" );
			Contract.RequiresNotNull( sphere, "sphere" );

			Vector3 rayDir = ray.Direction;
			//Adjust ray origin relative to sphere center
			Vector3 rayOrig = ray.Origin - sphere.Center;
			Real radius = sphere.Radius;

			// check origin inside first
			if ( ( rayOrig.LengthSquared <= radius * radius ) && discardInside )
			{
				return new IntersectResult( true, 0 );
			}

			// mmm...sweet quadratics
			// Build coeffs which can be used with std quadratic solver
			// ie t = (-b +/- sqrt(b*b* + 4ac)) / 2a
			Real a = rayDir.Dot( rayDir );
			Real b = 2 * rayOrig.Dot( rayDir );
			Real c = rayOrig.Dot( rayOrig ) - ( radius * radius );

			// calc determinant
			Real d = ( b * b ) - ( 4 * a * c );

			if ( d < 0 )
			{
				// no intersection
				return new IntersectResult( false, 0 );
			}
			else
			{
				// BTW, if d=0 there is one intersection, if d > 0 there are 2
				// But we only want the closest one, so that's ok, just use the 
				// '-' version of the solver
				Real t = ( -b - Utility.Sqrt( d ) ) / ( 2 * a );

				if ( t < 0 )
				{
					t = ( -b + Utility.Sqrt( d ) ) / ( 2 * a );
				}

				return new IntersectResult( true, t );
			}
		}

		/// <summary>
		///		Ray/Plane intersection test.
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="plane"></param>
		/// <returns>Struct that contains a bool (hit?) and distance.</returns>
		public static IntersectResult Intersects( Ray ray, Plane plane )
		{
			Contract.RequiresNotNull( ray, "ray" );

			Real denom = plane.Normal.Dot( ray.Direction );

			if ( Utility.Abs( denom ) < Real.Epsilon )
			{
				// Parellel
				return new IntersectResult( false, 0 );
			}
			else
			{
				Real nom = plane.Normal.Dot( ray.Origin ) + plane.D;
				Real t = -( nom / denom );
				return new IntersectResult( t >= 0, t );
			}
		}

		/// <summary>
		///		Sphere/Box intersection test.
		/// </summary>
		/// <param name="sphere"></param>
		/// <param name="box"></param>
		/// <returns>True if there was an intersection, false otherwise.</returns>
		public static bool Intersects( Sphere sphere, AxisAlignedBox box )
		{
			Contract.RequiresNotNull( sphere, "sphere" );
			Contract.RequiresNotNull( box, "box" );

			if ( box.IsNull )
				return false;

			// Use splitting planes
			Vector3 center = sphere.Center;
			Real radius = sphere.Radius;
			Vector3 min = box.Minimum;
			Vector3 max = box.Maximum;

			// just test facing planes, early fail if sphere is totally outside
			if ( center.x < min.x &&
				min.x - center.x > radius )
			{
				return false;
			}
			if ( center.x > max.x &&
				center.x - max.x > radius )
			{
				return false;
			}

			if ( center.y < min.y &&
				min.y - center.y > radius )
			{
				return false;
			}
			if ( center.y > max.y &&
				center.y - max.y > radius )
			{
				return false;
			}

			if ( center.z < min.z &&
				min.z - center.z > radius )
			{
				return false;
			}
			if ( center.z > max.z &&
				center.z - max.z > radius )
			{
				return false;
			}

			// Must intersect
			return true;
		}

		/// <summary>
		///		Plane/Box intersection test.
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="box"></param>
		/// <returns>True if there was an intersection, false otherwise.</returns>
		public static bool Intersects( Plane plane, AxisAlignedBox box )
		{
			Contract.RequiresNotNull( box, "box" );

			if ( box.IsNull )
				return false;

			// Get corners of the box
			Vector3[] corners = box.Corners;

			// Test which side of the plane the corners are
			// Intersection occurs when at least one corner is on the 
			// opposite side to another
			PlaneSide lastSide = plane.GetSide( corners[ 0 ] );

			for ( int corner = 1; corner < 8; corner++ )
			{
				if ( plane.GetSide( corners[ corner ] ) != lastSide )
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		///		Sphere/Plane intersection test.
		/// </summary>
		/// <param name="sphere"></param>
		/// <param name="plane"></param>
		/// <returns>True if there was an intersection, false otherwise.</returns>
		public static bool Intersects( Sphere sphere, Plane plane )
		{
			Contract.RequiresNotNull( sphere, "sphere" );

			return Utility.Abs( plane.Normal.Dot( sphere.Center ) ) <= sphere.Radius;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="box"></param>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		public static Tuple<bool, Real, Real> Intersect( Ray ray, AxisAlignedBox box )
		{
			if ( box.IsNull )
				return new Tuple<bool, Real, Real>( false, Real.NaN, Real.NaN );

			if ( box.IsInfinite )
			{
				return new Tuple<bool, Real, Real>( true, Real.NaN, Real.PositiveInfinity );
			}

			Vector3 min = box.Minimum;
			Vector3 max = box.Maximum;
			Vector3 rayorig = ray.origin;
			Vector3 rayDir = ray.Direction;

			Vector3 absDir = Vector3.Zero;
			absDir[ 0 ] = Abs( rayDir[ 0 ] );
			absDir[ 1 ] = Abs( rayDir[ 1 ] );
			absDir[ 2 ] = Abs( rayDir[ 2 ] );

			// Sort the axis, ensure check minimise floating error axis first
			int imax = 0, imid = 1, imin = 2;
			if ( absDir[ 0 ] < absDir[ 2 ] )
			{
				imax = 2;
				imin = 0;
			}
			if ( absDir[ 1 ] < absDir[ imin ] )
			{
				imid = imin;
				imin = 1;
			}
			else if ( absDir[ 1 ] > absDir[ imax ] )
			{
				imid = imax;
				imax = 1;
			}

			Real start = 0, end = Real.PositiveInfinity;
			// Check each axis in turn

			if ( !CalcAxis( imax, rayDir, rayorig, min, max, ref end, ref start ) )
				return new Tuple<bool, Real, Real>( false, Real.NaN, Real.NaN );

			if ( absDir[ imid ] < Real.Epsilon )
			{
				// Parallel with middle and minimise axis, check bounds only
				if ( rayorig[ imid ] < min[ imid ] || rayorig[ imid ] > max[ imid ] ||
					rayorig[ imin ] < min[ imin ] || rayorig[ imin ] > max[ imin ] )
					return new Tuple<bool, Real, Real>( false, Real.NaN, Real.NaN );
			}
			else
			{
				if ( !CalcAxis( imid, rayDir, rayorig, min, max, ref end, ref start ) )
					return new Tuple<bool, Real, Real>( false, Real.NaN, Real.NaN );

				if ( absDir[ imin ] < Real.Epsilon )
				{
					// Parallel with minimise axis, check bounds only
					if ( rayorig[ imin ] < min[ imin ] || rayorig[ imin ] > max[ imin ] )
						return new Tuple<bool, Real, Real>( false, Real.NaN, Real.NaN );
				}
				else
				{
					if ( !CalcAxis( imin, rayDir, rayorig, min, max, ref end, ref start ) )
						return new Tuple<bool, Real, Real>( false, Real.NaN, Real.NaN );
				}
			}
			return new Tuple<bool, Real, Real>( true, start, end );
		}

		private static bool CalcAxis( int i, Vector3 raydir, Vector3 rayorig, Vector3 min, Vector3 max, ref Real end, ref Real start )
		{
			Real denom = 1 / raydir[ i ];
			Real newstart = ( min[ i ] - rayorig[ i ] ) * denom;
			Real newend = ( max[ i ] - rayorig[ i ] ) * denom;
			if ( newstart > newend )
				Swap<Real>( ref newstart, ref newend );
			if ( newstart > end || newend < start )
				return false;
			if ( newstart > start )
				start = newstart;
			if ( newend < end )
				end = newend;

			return true;
		}

		/// <summary>
		///    Ray/PlaneBoundedVolume intersection test.
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="volume"></param>
		/// <returns>Struct that contains a bool (hit?) and distance.</returns>
		public static IntersectResult Intersects( Ray ray, PlaneBoundedVolume volume )
		{
			Contract.RequiresNotNull( ray, "ray" );
			Contract.RequiresNotNull( volume, "volume" );

			PlaneList planes = volume.planes;

			Real maxExtDist = 0.0f;
			Real minIntDist = Real.PositiveInfinity;

			Real dist, denom, nom;

			for ( int i = 0; i < planes.Count; i++ )
			{
				Plane plane = (Plane)planes[ i ];

				denom = plane.Normal.Dot( ray.Direction );
				if ( Utility.Abs( denom ) < Real.Epsilon )
				{
					// Parallel
					if ( plane.GetSide( ray.Origin ) == volume.outside )
						return new IntersectResult( false, 0 );

					continue;
				}

				nom = plane.Normal.Dot( ray.Origin ) + plane.D;
				dist = -( nom / denom );

				if ( volume.outside == PlaneSide.Negative )
					nom = -nom;

				if ( dist > 0.0f )
				{
					if ( nom > 0.0f )
					{
						if ( maxExtDist < dist )
							maxExtDist = dist;
					}
					else
					{
						if ( minIntDist > dist )
							minIntDist = dist;
					}
				}
				else
				{
					//Ray points away from plane
					if ( volume.outside == PlaneSide.Negative )
						denom = -denom;

					if ( denom > 0.0f )
						return new IntersectResult( false, 0 );
				}
			}

			if ( maxExtDist > minIntDist )
				return new IntersectResult( false, 0 );

			return new IntersectResult( true, maxExtDist );
		}

		#endregion Intersection Methods

		/// <summary>
		/// Swaps two values
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		public static void Swap<T>( ref T v1, ref T v2 )
		{
			T temp = v1;
			v1 = v2;
			v2 = temp;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="max"></param>
		/// <param name="min"></param>
		/// <returns></returns>
		public static T Clamp<T>( T value, T max, T min )
				 where T : System.IComparable<T>
		{
			T result = value;
			if ( value.CompareTo( max ) > 0 )
				result = max;
			if ( value.CompareTo( min ) < 0 )
				result = min;
			return result;
		}

		public static T Max<T>( T value, T max )
		 where T : System.IComparable<T>
		{
			T result = value;
			if ( value.CompareTo( max ) < 0 )
				result = max;
			return result;
		}

		public static T Min<T>( T value, T min )
				 where T : System.IComparable<T>
		{
			T result = value;
			if ( value.CompareTo( min ) > 0 )
				result = min;
			return result;
		}

		public static Real Sqr( Real number )
		{
			return number * number;
		}

		public static int Log2( int x )
		{
			if ( x <= 65536 )
			{
				if ( x <= 256 )
				{
					if ( x <= 16 )
					{
						if ( x <= 4 )
						{
							if ( x <= 2 )
							{
								if ( x <= 1 )
								{
									return 0;
								}
								return 1;
							}
							return 2;
						}
						if ( x <= 8 )
							return 3;
						return 4;
					}
					if ( x <= 64 )
					{
						if ( x <= 32 )
							return 5;
						return 6;
					}
					if ( x <= 128 )
						return 7;
					return 8;
				}
				if ( x <= 4096 )
				{
					if ( x <= 1024 )
					{
						if ( x <= 512 )
							return 9;
						return 10;
					}
					if ( x <= 2048 )
						return 11;
					return 12;
				}
				if ( x <= 16384 )
				{
					if ( x <= 8192 )
						return 13;
					return 14;
				}
				if ( x <= 32768 )
					return 15;
				return 16;
			}
			if ( x <= 16777216 )
			{
				if ( x <= 1048576 )
				{
					if ( x <= 262144 )
					{
						if ( x <= 131072 )
							return 17;
						return 18;
					}
					if ( x <= 524288 )
						return 19;
					return 20;
				}
				if ( x <= 4194304 )
				{
					if ( x <= 2097152 )
						return 21;
					return 22;
				}
				if ( x <= 8388608 )
					return 23;
				return 24;
			}
			if ( x <= 268435456 )
			{
				if ( x <= 67108864 )
				{
					if ( x <= 33554432 )
						return 25;
					return 26;
				}
				if ( x <= 134217728 )
					return 27;
				return 28;
			}
			if ( x <= 1073741824 )
			{
				if ( x <= 536870912 )
					return 29;
				return 30;
			}
			//	since int is unsigned it can never be higher than 2,147,483,647
			//	if( x <= 2147483648 )
			//		return	31;	
			//	return	32;	
			return 31;
		}

		/// <summary>
		/// Generates a value based on the Gaussian (normal) distribution function
		/// with the given offset and scale parameters.
		/// </summary>
		/// <returns></returns>
		public static float GaussianDistribution( Real x, Real offset, Real scale )
		{
			Real nom = System.Math.Exp( -Utility.Sqr( x - offset ) / ( 2 * Utility.Sqr( scale ) ) );
			Real denom = scale * Utility.Sqrt( 2 * Utility.PI );

			return nom / denom;
		}
	}

	#region Return result structures

	/// <summary>
	///		Simple struct to allow returning a complex intersection result.
	/// </summary>
	public struct IntersectResult
	{
		#region Fields

		/// <summary>
		///		Did the intersection test result in a hit?
		/// </summary>
		public bool Hit;

		/// <summary>
		///		If Hit was true, this will hold a query specific distance value.
		///		i.e. for a Ray-Box test, the distance will be the distance from the start point
		///		of the ray to the point of intersection.
		/// </summary>
		public Real Distance;

		#endregion Fields

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="hit"></param>
		/// <param name="distance"></param>
		public IntersectResult( bool hit, Real distance )
		{
			this.Hit = hit;
			this.Distance = distance;
		}
	}

	#endregion Return result structures
}


