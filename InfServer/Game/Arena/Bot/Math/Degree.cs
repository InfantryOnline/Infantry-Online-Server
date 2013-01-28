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
	/// Wrapper class which indicates a given angle value is in Radian.
	/// </summary>
	/// <remarks>
	/// Degree values are interchangeable with Radian values, and conversions
	/// will be done automatically between them.
	/// </remarks>
	[StructLayout( LayoutKind.Sequential )]
#if !( XBOX || XBOX360 )
	[Serializable]
	public struct Degree : ISerializable, IComparable<Degree>, IComparable<Radian>, IComparable<Real>
#else
	public struct Degree : IComparable<Degree>, IComparable<Radian>, IComparable<Real>
#endif
	{
		private static readonly Real _degreesToRadians = Utility.PI / 180.0f;

		public static readonly Degree Zero = (Degree)Real.Zero;

		private Real _value;

		public Degree( Real r )
		{
			_value = r;
		}
		public Degree( Degree d )
		{
			_value = d._value;
		}
		public Degree( Radian r )
		{
			_value = r.InDegrees;
		}

		public Radian InRadians
		{
			get
			{
				return _value * _degreesToRadians;
			}
		}

		public static implicit operator Degree( Real value )
		{
			Degree retVal;
			retVal._value = value;
			return retVal;
		}
		public static implicit operator Degree( Radian value )
		{
			Degree retVal;
			retVal._value = value;
			return retVal;
		}
		public static implicit operator Degree( Numeric value )
		{
			Degree retVal;
			retVal._value = value;
			return retVal;
		}
		public static explicit operator Degree( int value )
		{
			Degree retVal;
			retVal._value = value;
			return retVal;
		}

		public static implicit operator Real( Degree value )
		{
			return (Real)value._value;
		}
		public static explicit operator Numeric( Degree value )
		{
			return (Numeric)value._value;
		}

		public static Degree operator +( Degree left, Real right )
		{
			return left._value + right;
		}
		public static Degree operator +( Degree left, Degree right )
		{
			return left._value + right._value;
		}
		public static Degree operator +( Degree left, Radian right )
		{
			return left + right.InDegrees;
		}

		public static Degree operator -( Degree r )
		{
			return -r._value;
		}
		public static Degree operator -( Degree left, Real right )
		{
			return left._value - right;
		}
		public static Degree operator -( Degree left, Degree right )
		{
			return left._value - right._value;
		}
		public static Degree operator -( Degree left, Radian right )
		{
			return left - right.InDegrees;
		}

		public static Degree operator *( Degree left, Real right )
		{
			return left._value * right;
		}
		public static Degree operator *( Real left, Degree right )
		{
			return left * right._value;
		}
		public static Degree operator *( Degree left, Degree right )
		{
			return left._value * right._value;
		}
		public static Degree operator *( Degree left, Radian right )
		{
			return left._value * right.InDegrees;
		}

		public static Degree operator /( Degree left, Real right )
		{
			return left._value / right;
		}

		public static bool operator <( Degree left, Degree right )
		{
			return left._value < right._value;
		}
		public static bool operator ==( Degree left, Degree right )
		{
			return left._value == right._value;
		}
		public static bool operator !=( Degree left, Degree right )
		{
			return left._value != right._value;
		}
		public static bool operator >( Degree left, Degree right )
		{
			return left._value > right._value;
		}

		public override bool Equals( object obj )
		{
			return ( obj is Degree && this == (Degree)obj );
		}
		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

#if !( XBOX || XBOX360 )
		#region ISerializable Implementation
		private Degree( SerializationInfo info, StreamingContext context )
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

		public int CompareTo( Degree other )
		{
			return this._value.CompareTo( other );
		}
		public int CompareTo( Radian other )
		{
			return this._value.CompareTo( other.InDegrees );
		}
		public int CompareTo( Real other )
		{
			return this._value.CompareTo( other );
		}

		#endregion

	}
}
