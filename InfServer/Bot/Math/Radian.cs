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
//     <id value="$Id: GpuProgramParameters.cs 1036 2007-04-27 02:56:41Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

#if AXIOM_REAL_AS_SINGLE || !( AXIOM_REAL_AS_DOUBLE )
using Numeric = System.Single;
#else
using Numeric = System.Double;
#endif

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	/// Wrapper class which indicates a given angle value is in Radians.
	/// </summary>
	/// <remarks>
	/// Radian values are interchangeable with Degree values, and conversions
	/// will be done automatically between them.
	/// </remarks>
	[StructLayout( LayoutKind.Sequential )]
#if !( XBOX || XBOX360 )
	[Serializable]
	public struct Radian : ISerializable, IComparable<Radian>, IComparable<Degree>, IComparable<Real>
#else
	public struct Radian : IComparable<Radian>, IComparable<Degree>, IComparable<Real>
#endif
	{
		private static readonly Real _radiansToDegrees = 180.0f / Utility.PI;

		public static readonly Radian Zero = (Radian)Real.Zero;

		private Real _value;

		public Radian( Real r )
		{
			_value = r;
		}
		public Radian( Radian r )
		{
			_value = r._value;
		}
		public Radian( Degree d )
		{
			_value = d.InRadians;
		}

		public Degree InDegrees
		{
			get
			{
				return _value * _radiansToDegrees;
			}
		}

		public static implicit operator Radian( Real value )
		{
			Radian retVal;
			retVal._value = value;
			return retVal;
		}
		public static implicit operator Radian( Degree value )
		{
			Radian retVal;
			retVal._value = value;
			return retVal;
		}
		public static implicit operator Radian( Numeric value )
		{
			Radian retVal;
			retVal._value = value;
			return retVal;
		}
		public static explicit operator Radian( int value )
		{
			Radian retVal;
			retVal._value = value;
			return retVal;
		}

		public static implicit operator Real( Radian value )
		{
			return (Real)value._value;
		}
		public static explicit operator Numeric( Radian value )
		{
			return (Numeric)value._value;
		}

		public static Radian operator +( Radian left, Real right )
		{
			return left._value + right;
		}
		public static Radian operator +( Radian left, Radian right )
		{
			return left._value + right._value;
		}
		public static Radian operator +( Radian left, Degree right )
		{
			return left + right.InRadians;
		}

		public static Radian operator -( Radian r )
		{
			return -r._value;
		}
		public static Radian operator -( Radian left, Real right )
		{
			return left._value - right;
		}
		public static Radian operator -( Radian left, Radian right )
		{
			return left._value - right._value;
		}
		public static Radian operator -( Radian left, Degree right )
		{
			return left - right.InRadians;
		}

		public static Radian operator *( Radian left, Real right )
		{
			return left._value * right;
		}
		public static Radian operator *( Real left, Radian right )
		{
			return left * right._value;
		}
		public static Radian operator *( Radian left, Radian right )
		{
			return left._value * right._value;
		}
		public static Radian operator *( Radian left, Degree right )
		{
			return left._value * right.InRadians;
		}

		public static Radian operator /( Radian left, Real right )
		{
			return left._value / right;
		}

		public static bool operator <( Radian left, Radian right )
		{
			return left._value < right._value;
		}
		public static bool operator ==( Radian left, Radian right )
		{
			return left._value == right._value;
		}
		public static bool operator !=( Radian left, Radian right )
		{
			return left._value != right._value;
		}
		public static bool operator >( Radian left, Radian right )
		{
			return left._value > right._value;
		}

		public override bool Equals( object obj )
		{
			return ( obj is Radian && this == (Radian)obj );
		}
		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

#if !( XBOX || XBOX360 )
		#region ISerializable Implementation
		private Radian( SerializationInfo info, StreamingContext context )
		{
			_value = (Real)info.GetValue( "value", typeof( Real ) );
		}

		[SecurityPermission( SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter )]
		public void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "value", _value );
		}
		#endregion ISerializableImplementation
#endif

		#region IComparable<T> Members

		public int CompareTo( Radian other )
		{
			return this._value.CompareTo( other._value );
		}
		public int CompareTo( Degree other )
		{
			return this._value.CompareTo( other.InRadians );
		}
		public int CompareTo( Real other )
		{
			return this._value.CompareTo( other );
		}

		#endregion
	}
}
