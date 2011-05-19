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
//     <id value="$Id: Vector2.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Globalization;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	///     2 dimensional vector.
	/// </summary>
	[StructLayout( LayoutKind.Sequential )]
	public struct Vector2
	{
		#region Fields

		public Real x, y;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets length of this vector
		/// </summary>
		public Real Length
		{
			get
			{
				return Utility.Sqrt( x * x + y * y );
			}
		}

		/// <summary>
		/// Gets the squared length of this vector
		/// </summary>
		public Real LengthSquared
		{
			get
			{
				return x * x + y * y;
			}
		}

		/// <summary>
		/// Gets a vector perpendicular to this, which has the same magnitude.
		/// </summary>
		public Vector2 Perpendicular
		{
			get
			{
				return new Vector2( this.y, -this.x );
			}
		}

		#endregion

		#region Static

		private static readonly Vector2 zeroVector = new Vector2( 0.0f, 0.0f );
		/// <summary>
		///		Gets a Vector2 with all components set to 0.
		/// </summary>
		public static Vector2 Zero
		{
			get
			{
				return zeroVector;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Constructor.
		/// </summary>
		/// <param name="x">X position.</param>
		/// <param name="y">Y position</param>
		public Vector2( Real x, Real y )
		{
			this.x = x;
			this.y = y;
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		///		Normalizes the vector.
		/// </summary>
		/// <remarks>
		///		This method Normalizes the vector such that it's
		///		length / magnitude is 1. The result is called a unit vector.
		///		<p/>
		///		This function will not crash for zero-sized vectors, but there
		///		will be no changes made to their components.
		///	</remarks>
		///	<returns>The previous length of the vector.</returns>
		public Real Normalize()
		{
			Real length = Utility.Sqrt( this.x * this.x + this.y * this.y );

			// Will also work for zero-sized vectors, but will change nothing
			if ( length > Real.Epsilon )
			{
				Real inverseLength = 1.0f / length;

				this.x *= inverseLength;
				this.y *= inverseLength;
			}

			return length;
		}

		/// <summary>
		/// Gets a normalized (unit length) vector of this vector
		/// </summary>
		/// <returns></returns>
		public Vector2 ToNormalized()
		{
			Vector2 vec = this;
			vec.Normalize();

			return vec;
		}

		/// <summary>
		/// Calculates the 2 dimensional cross-product of 2 vectors, which results
		/// in a Real value which is 2 times the area of the triangle
		/// defined by the two vectors. It also is the magnitude of the 3D vector that is perpendicular
		/// to the 2D vectors if the 2D vectors are projected to 3D space.
		/// </summary>
		/// <param name="vector"></param>
		/// <returns></returns>
		public Real Cross( Vector2 vector )
		{
			return this.x * vector.y - this.y * vector.x;
		}

		/// <summary>
		/// Calculates the 2 dimensional dot-product of 2 vectors, 
		/// which is equal to the cosine of the angle between the vectors, times the lengths of each of the vectors.
		/// A.Dot(B) == |A| * |B| * cos(fi)
		/// </summary>
		/// <param name="vector"></param>
		/// <returns></returns>
		public Real Dot( Vector2 vector )
		{
			return this.x * vector.x + this.y * vector.y;
		}

		#endregion

		#region CLS compliant methods and operator overloads

		/// <summary>
		///		Used when a Vector2 is added to another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 Add( Vector2 left, Vector2 right )
		{
			return left + right;
		}

		public static bool operator ==( Vector2 left, Vector2 right )
		{
			return left.x == right.x && left.y == right.y;
		}

		public static bool operator !=( Vector2 left, Vector2 right )
		{
			return left.x != right.x || left.y != right.y;
		}
		public override bool Equals( object obj )
		{
			return obj is Vector2 && this == (Vector2)obj;
		}

		/// <summary>
		///		Used when a Vector2 is added to another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator +( Vector2 left, Vector2 right )
		{
			return new Vector2( left.x + right.x, left.y + right.y );
		}

		/// <summary>
		///		Used to subtract a Vector2 from another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 Subtract( Vector2 left, Vector2 right )
		{
			return left - right;
		}

		/// <summary>
		///		Used to subtract a Vector2 from another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator -( Vector2 left, Vector2 right )
		{
			return new Vector2( left.x - right.x, left.y - right.y );
		}

		public static Vector2 operator *( Vector2 left, Vector2 right )
		{
			left.x *= right.x;
			left.y *= right.y;
			return left;
		}

		/// <summary>
		///		Used when a Vector2 is multiplied by a scalar value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Vector2 Multiply( Vector2 left, Real scalar )
		{
			return left * scalar;
		}

		/// <summary>
		///		Used when a Vector2 is multiplied by a scalar value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Vector2 operator *( Vector2 left, Real scalar )
		{
			return new Vector2( left.x * scalar, left.y * scalar );
		}

		/// <summary>
		///		Used when a scalar value is multiplied by a Vector2.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 Multiply( Real scalar, Vector2 right )
		{
			return scalar * right;
		}

		/// <summary>
		///		Used when a scalar value is multiplied by a Vector2.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator *( Real scalar, Vector2 right )
		{
			return new Vector2( right.x * scalar, right.y * scalar );
		}

		/// <summary>
		///		Used to negate the elements of a vector.
		/// </summary>
		/// <param name="left"></param>
		/// <returns></returns>
		public static Vector2 Negate( Vector2 left )
		{
			return -left;
		}

		/// <summary>
		///		Used to negate the elements of a vector.
		/// </summary>
		/// <param name="left"></param>
		/// <returns></returns>
		public static Vector2 operator -( Vector2 left )
		{
			return new Vector2( -left.x, -left.y );
		}

		#endregion

		#region Object overrides

		public override int GetHashCode()
		{
			return x.GetHashCode() ^ y.GetHashCode();
		}

		public override string ToString()
		{
			return String.Format( CultureInfo.InvariantCulture, "Vector2({0}, {1})", this.x, this.y );
		}

		#endregion

		#region Parse from string

		public Vector2 Parse( string s )
		{
			// the format is "Vector2(x, y)"
			if ( !s.StartsWith( "Vector2(" ) )
				throw new FormatException();

			string[] values = s.Substring( 8 ).TrimEnd( '}' ).Split( ',' );

			return new Vector2( Real.Parse( values[ 0 ], CultureInfo.InvariantCulture ),
							   Real.Parse( values[ 1 ], CultureInfo.InvariantCulture ) );
		}

		#endregion

		#region Infantry Related
		/// <summary>
		///	Creates a unit vector based on the given direction
		/// </summary>
		public static Vector2 createUnitVector(double direction)
		{
			direction *= 1.5;
			direction = direction * (Utility.PI / 180);

			double x = System.Math.Sin(direction);
			double y = -System.Math.Cos(direction);
			return new Vector2(x, y);
		}

		/// <summary>
		///	Normalizes the vector for the given magnitude
		/// </summary>
		public void Normalize(double magnitude)
		{
			double difference = magnitude / this.Length;

			this.x *= difference;
			this.y *= difference;
		}
		#endregion
	}
}
