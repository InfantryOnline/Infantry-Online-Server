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
//     <id value="$Id: Vector4.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	/// 4D homogeneous vector.
	/// </summary>
	[StructLayout( LayoutKind.Sequential )]
	public struct Vector4
	{
		#region Member variables

		public Real x, y, z, w;

		private static readonly Vector4 zeroVector = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f );

		#endregion

		#region Constructors

		/// <summary>
		///		Creates a new 4 dimensional Vector.
		/// </summary>
		public Vector4( Real x, Real y, Real z, Real w )
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets a Vector4 with all components set to 0.
		/// </summary>
		public static Vector4 Zero
		{
			get
			{
				return zeroVector;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///     Calculates the dot (scalar) product of this vector with another.
		/// </summary>
		/// <param name="vec">
		///     Vector with which to calculate the dot product (together with this one).
		/// </param>
		/// <returns>A Real representing the dot product value.</returns>
		public Real Dot( Vector4 vec )
		{
			return x * vec.x + y * vec.y + z * vec.z + w * vec.w;
		}

		#endregion Methods

		#region Operator overloads + CLS compliant method equivalents

		/// <summary>
		///		
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector4 Multiply( Vector4 vector, Matrix4 matrix )
		{
			return vector * matrix;
		}
		/// <summary>
		///		
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector4 operator *( Matrix4 matrix, Vector4 vector )
		{
			Vector4 result = new Vector4();

			result.x = vector.x * matrix.m00 + vector.y * matrix.m01 + vector.z * matrix.m02 + vector.w * matrix.m03;
			result.y = vector.x * matrix.m10 + vector.y * matrix.m11 + vector.z * matrix.m12 + vector.w * matrix.m13;
			result.z = vector.x * matrix.m20 + vector.y * matrix.m21 + vector.z * matrix.m22 + vector.w * matrix.m23;
			result.w = vector.x * matrix.m30 + vector.y * matrix.m31 + vector.z * matrix.m32 + vector.w * matrix.m33;

			return result;
		}

		// TODO: Find the signifance of having 2 overloads with opposite param lists that do transposed operations
		public static Vector4 operator *( Vector4 vector, Matrix4 matrix )
		{
			Vector4 result = new Vector4();

			result.x = vector.x * matrix.m00 + vector.y * matrix.m10 + vector.z * matrix.m20 + vector.w * matrix.m30;
			result.y = vector.x * matrix.m01 + vector.y * matrix.m11 + vector.z * matrix.m21 + vector.w * matrix.m31;
			result.z = vector.x * matrix.m02 + vector.y * matrix.m12 + vector.z * matrix.m22 + vector.w * matrix.m32;
			result.w = vector.x * matrix.m03 + vector.y * matrix.m13 + vector.z * matrix.m23 + vector.w * matrix.m33;

			return result;
		}

		/// <summary>
		///		Multiplies a Vector4 by a scalar value.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Vector4 operator *( Vector4 vector, Real scalar )
		{
			Vector4 result = new Vector4();

			result.x = vector.x * scalar;
			result.y = vector.y * scalar;
			result.z = vector.z * scalar;
			result.w = vector.w * scalar;

			return result;
		}

		/// <summary>
		///		User to compare two Vector4 instances for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>true or false</returns>
		public static bool operator ==( Vector4 left, Vector4 right )
		{
			return ( left.x == right.x &&
				left.y == right.y &&
				left.z == right.z &&
				left.w == right.w );
		}

		/// <summary>
		///		Used to add a Vector4 to another Vector4.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector4 operator +( Vector4 left, Vector4 right )
		{
			return new Vector4( left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w );
		}

		/// <summary>
		///		Used to subtract a Vector4 from another Vector4.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector4 operator -( Vector4 left, Vector4 right )
		{
			return new Vector4( left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w );
		}
		/// <summary>
		///		Used to negate the elements of a vector.
		/// </summary>
		/// <param name="left"></param>
		/// <returns></returns>
		public static Vector4 operator -( Vector4 left )
		{
			return new Vector4( -left.x, -left.y, -left.z, -left.w );
		}

		/// <summary>
		///		User to compare two Vector4 instances for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>true or false</returns>
		public static bool operator !=( Vector4 left, Vector4 right )
		{
			return ( left.x != right.x ||
				left.y != right.y ||
				left.z != right.z ||
				left.w != right.w );
		}

		/// <summary>
		///		Used to access a Vector by index 0 = this.x, 1 = this.y, 2 = this.z, 3 = this.w.  
		/// </summary>
		/// <remarks>
		///		Uses unsafe pointer arithmetic to reduce the code required.
		///	</remarks>
		public Real this[ int index ]
		{
			get
			{
				Debug.Assert( index >= 0 && index < 4, "Indexer boundaries overrun in Vector4." );

				// using pointer arithmetic here for less code.  Otherwise, we'd have a big switch statement.
				unsafe
				{
					fixed ( Real* pX = &x )
						return *( pX + index );
				}
			}
			set
			{
				Debug.Assert( index >= 0 && index < 4, "Indexer boundaries overrun in Vector4." );

				// using pointer arithmetic here for less code.  Otherwise, we'd have a big switch statement.
				unsafe
				{
					fixed ( Real* pX = &x )
						*( pX + index ) = value;
				}
			}
		}

		#endregion

		#region Object overloads

		/// <summary>
		///		Overrides the Object.ToString() method to provide a text representation of 
		///		a Vector4.
		/// </summary>
		/// <returns>A string representation of a Vector4.</returns>
		public override string ToString()
		{
			return string.Format( "<{0},{1},{2},{3}>", this.x, this.y, this.z, this.w );
		}

		/// <summary>
		///		Provides a unique hash code based on the member variables of this
		///		class.  This should be done because the equality operators (==, !=)
		///		have been overriden by this class.
		///		<p/>
		///		The standard implementation is a simple XOR operation between all local
		///		member variables.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() ^ this.z.GetHashCode() ^ this.w.GetHashCode();
		}

		/// <summary>
		///		Compares this Vector to another object.  This should be done because the 
		///		equality operators (==, !=) have been overriden by this class.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals( object obj )
		{
			return obj is Vector4 && this == (Vector4)obj;
		}

		#endregion

		#region Parse method, implemented for factories

		/// <summary>
		///		Parses a string and returns Vector4.
		/// </summary>
		/// <param name="vector">
		///     A string representation of a Vector4. ( as its returned from Vector4.ToString() )
		/// </param>
		/// <returns>
		///     A new Vector4.
		/// </returns>
		public static Vector4 Parse( string vector )
		{
			string[] vals = vector.TrimStart( '<' ).TrimEnd( '>' ).Split( ',' );

			return new Vector4( Real.Parse( vals[ 0 ].Trim() ), Real.Parse( vals[ 1 ].Trim() ), Real.Parse( vals[ 2 ].Trim() ), Real.Parse( vals[ 3 ].Trim() ) );
		}


		#endregion
	}
}
