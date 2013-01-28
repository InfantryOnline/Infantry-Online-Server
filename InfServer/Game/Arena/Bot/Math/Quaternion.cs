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
//     <id value="$Id: Quaternion.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Globalization;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	///		Summary description for Quaternion.
	/// </summary>
	public struct Quaternion
	{
		#region Private member variables and constants

		const float EPSILON = 1e-03f;

		public Real w, x, y, z;

		private static readonly Quaternion identityQuat = new Quaternion( 1.0f, 0.0f, 0.0f, 0.0f );
		private static readonly Quaternion zeroQuat = new Quaternion( 0.0f, 0.0f, 0.0f, 0.0f );
		private static readonly int[] next = new int[ 3 ] { 1, 2, 0 };

		#endregion

		#region Constructors

		//		public Quaternion()
		//		{
		//			this.w = 1.0f;
		//		}

		/// <summary>
		///		Creates a new Quaternion.
		/// </summary>
		public Quaternion( Real w, Real x, Real y, Real z )
		{
			this.w = w;
			this.x = x;
			this.y = y;
			this.z = z;
		}

		#endregion

		#region Operator Overloads + CLS compliant method equivalents

		/// <summary>
		/// Used to multiply 2 Quaternions together.
		/// </summary>
		/// <remarks>
		///		Quaternion multiplication is not communative in most cases.
		///		i.e. p*q != q*p
		/// </remarks>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion Multiply( Quaternion left, Quaternion right )
		{
			return left * right;
		}

		/// <summary>
		/// Used to multiply 2 Quaternions together.
		/// </summary>
		/// <remarks>
		///		Quaternion multiplication is not communative in most cases.
		///		i.e. p*q != q*p
		/// </remarks>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator *( Quaternion left, Quaternion right )
		{
			Quaternion q = new Quaternion();

			q.w = left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z;
			q.x = left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y;
			q.y = left.w * right.y + left.y * right.w + left.z * right.x - left.x * right.z;
			q.z = left.w * right.z + left.z * right.w + left.x * right.y - left.y * right.x;

			/*
			return new Quaternion
				(
				left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z,
				left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y,
				left.w * right.y + left.y * right.w + left.z * right.x - left.x * right.z,
				left.w * right.z + left.z * right.w + left.x * right.y - left.y * right.x
				); */

			return q;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="quat"></param>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static Vector3 Multiply( Quaternion quat, Vector3 vector )
		{
			return quat * vector;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="quat"></param>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static Vector3 operator *( Quaternion quat, Vector3 vector )
		{
			// nVidia SDK implementation
			Vector3 uv, uuv;
			Vector3 qvec = new Vector3( quat.x, quat.y, quat.z );

			uv = qvec.Cross( vector );
			uuv = qvec.Cross( uv );
			uv *= ( 2.0f * quat.w );
			uuv *= 2.0f;

			return vector + uv + uuv;

			// get the rotation matrix of the Quaternion and multiply it times the vector
			//return quat.ToRotationMatrix() * vector;
		}

		/// <summary>
		/// Used when a Real value is multiplied by a Quaternion.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion Multiply( Real scalar, Quaternion right )
		{
			return scalar * right;
		}

		/// <summary>
		/// Used when a Real value is multiplied by a Quaternion.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator *( Real scalar, Quaternion right )
		{
			return new Quaternion( scalar * right.w, scalar * right.x, scalar * right.y, scalar * right.z );
		}

		/// <summary>
		/// Used when a Quaternion is multiplied by a Real value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Quaternion Multiply( Quaternion left, Real scalar )
		{
			return left * scalar;
		}

		/// <summary>
		/// Used when a Quaternion is multiplied by a Real value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Quaternion operator *( Quaternion left, Real scalar )
		{
			return new Quaternion( scalar * left.w, scalar * left.x, scalar * left.y, scalar * left.z );
		}

		/// <summary>
		/// Used when a Quaternion is added to another Quaternion.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion Add( Quaternion left, Quaternion right )
		{
			return left + right;
		}

		public static Quaternion Subtract( Quaternion left, Quaternion right )
		{
			return left - right;
		}

		/// <summary>
		/// Used when a Quaternion is added to another Quaternion.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator +( Quaternion left, Quaternion right )
		{
			return new Quaternion( left.w + right.w, left.x + right.x, left.y + right.y, left.z + right.z );
		}

		public static Quaternion operator -( Quaternion left, Quaternion right )
		{
			return new Quaternion( left.w - right.w, left.x - right.x, left.y - right.y, left.z - right.z );
		}

		/// <summary>
		///     Negates a Quaternion, which simply returns a new Quaternion
		///     with all components negated.
		/// </summary>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator -( Quaternion right )
		{
			return new Quaternion( -right.w, -right.x, -right.y, -right.z );
		}

		public static bool operator ==( Quaternion left, Quaternion right )
		{
			return ( left.w == right.w && left.x == right.x && left.y == right.y && left.z == right.z );
		}

		public static bool operator !=( Quaternion left, Quaternion right )
		{
			return !( left == right );
		}

		#endregion

		#region Properties

		/// <summary>
		///    An Identity Quaternion.
		/// </summary>
		public static Quaternion Identity
		{
			get
			{
				return identityQuat;
			}
		}

		/// <summary>
		///    A Quaternion with all elements set to 0.0f;
		/// </summary>
		public static Quaternion Zero
		{
			get
			{
				return zeroQuat;
			}
		}

		/// <summary>
		///		Squared 'length' of this quaternion.
		/// </summary>
		public Real Norm
		{
			get
			{
				return x * x + y * y + z * z + w * w;
			}
		}

		/// <summary>
		///    Local X-axis portion of this rotation.
		/// </summary>
		public Vector3 XAxis
		{
			get
			{
				Real fTx = 2.0f * x;
				Real fTy = 2.0f * y;
				Real fTz = 2.0f * z;
				Real fTwy = fTy * w;
				Real fTwz = fTz * w;
				Real fTxy = fTy * x;
				Real fTxz = fTz * x;
				Real fTyy = fTy * y;
				Real fTzz = fTz * z;

				return new Vector3( 1.0f - ( fTyy + fTzz ), fTxy + fTwz, fTxz - fTwy );
			}
		}

		/// <summary>
		///    Local Y-axis portion of this rotation.
		/// </summary>
		public Vector3 YAxis
		{
			get
			{
				Real fTx = 2.0f * x;
				Real fTy = 2.0f * y;
				Real fTz = 2.0f * z;
				Real fTwx = fTx * w;
				Real fTwz = fTz * w;
				Real fTxx = fTx * x;
				Real fTxy = fTy * x;
				Real fTyz = fTz * y;
				Real fTzz = fTz * z;

				return new Vector3( fTxy - fTwz, 1.0f - ( fTxx + fTzz ), fTyz + fTwx );
			}
		}

		/// <summary>
		///    Local Z-axis portion of this rotation.
		/// </summary>
		public Vector3 ZAxis
		{
			get
			{
				Real fTx = 2.0f * x;
				Real fTy = 2.0f * y;
				Real fTz = 2.0f * z;
				Real fTwx = fTx * w;
				Real fTwy = fTy * w;
				Real fTxx = fTx * x;
				Real fTxz = fTz * x;
				Real fTyy = fTy * y;
				Real fTyz = fTz * y;

				return new Vector3( fTxz + fTwy, fTyz - fTwx, 1.0f - ( fTxx + fTyy ) );
			}
		}
		public Real PitchInDegrees
		{
			get
			{
				return Utility.RadiansToDegrees( Pitch );
			}
			set
			{
				Pitch = Utility.DegreesToRadians( value );
			}
		}
		public Real YawInDegrees
		{
			get
			{
				return Utility.RadiansToDegrees( Yaw );
			}
			set
			{
				Yaw = Utility.DegreesToRadians( value );
			}
		}
		public Real RollInDegrees
		{
			get
			{
				return Utility.RadiansToDegrees( Roll );
			}
			set
			{
				Roll = Utility.DegreesToRadians( value );
			}
		}

		public Real Pitch
		{
			set
			{
				Real pitch, yaw, roll;
				ToEulerAngles( out pitch, out yaw, out roll );
				this = FromEulerAngles( value, yaw, roll );
			}
			get
			{
				Real test = x * y + z * w;
				if ( Utility.Abs( test ) > 0.499f ) // singularity at north and south pole
					return 0f;
				return (Real)Utility.ATan2( 2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z );
			}
		}


		public Real Yaw
		{
			set
			{
				Real pitch, yaw, roll;
				ToEulerAngles( out pitch, out yaw, out roll );
				this = FromEulerAngles( pitch, value, roll );
			}
			get
			{
				Real test = x * y + z * w;
				if ( Utility.Abs( test ) > 0.499f ) // singularity at north and south pole
					return Utility.Sign( test ) * 2 * Utility.ATan2( x, w );
				return Utility.ATan2( 2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z );
			}
		}
		public Real Roll
		{
			set
			{

				Real pitch, yaw, roll;
				ToEulerAngles( out pitch, out yaw, out roll );
				this = FromEulerAngles( pitch, yaw, value );
			}
			get
			{
				Real test = x * y + z * w;
				if ( Utility.Abs( test ) > 0.499f ) // singularity at north and south pole
					return Utility.Sign( test ) * Utility.PI / 2;
				return (Real)Utility.ASin( 2 * test );
			}
		}


		#endregion

		#region Static methods

		public static Quaternion Slerp( Real time, Quaternion quatA, Quaternion quatB )
		{
			return Slerp( time, quatA, quatB, false );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="quatA"></param>
		/// <param name="quatB"></param>
		/// <param name="useShortestPath"></param>
		/// <returns></returns>
		public static Quaternion Slerp( Real time, Quaternion quatA, Quaternion quatB, bool useShortestPath )
		{
			Real cos = quatA.Dot( quatB );

			Real angle = (Real)Utility.ACos( cos );

			if ( Utility.Abs( angle ) < EPSILON )
			{
				return quatA;
			}

			Real sin = Utility.Sin( angle );
			Real inverseSin = 1.0f / sin;
			Real coeff0 = Utility.Sin( ( 1.0f - time ) * angle ) * inverseSin;
			Real coeff1 = Utility.Sin( time * angle ) * inverseSin;

			Quaternion result;

			if ( cos < 0.0f && useShortestPath )
			{
				coeff0 = -coeff0;
				// taking the complement requires renormalisation
				Quaternion t = coeff0 * quatA + coeff1 * quatB;
				t.Normalize();
				result = t;
			}
			else
			{
				result = ( coeff0 * quatA + coeff1 * quatB );
			}

			return result;
		}

		/// <overloads><summary>
		/// Normalized linear interpolation - faster but less accurate (non-constant rotation velocity)
		/// </summary>
		/// <param name="fT"></param>
		/// <param name="rkP"></param>
		/// <param name="rkQ"></param>
		/// <returns></returns>
		/// </overloads>
		public static Quaternion Nlerp( Real fT, Quaternion rkP, Quaternion rkQ )
		{
			return Nlerp( fT, rkP, rkQ, false );
		}


		/// <param name="shortestPath"></param>
		public static Quaternion Nlerp( Real fT, Quaternion rkP, Quaternion rkQ, bool shortestPath )
		{
			Quaternion result;
			Real fCos = rkP.Dot( rkQ );
			if ( fCos < 0.0f && shortestPath )
			{
				result = rkP + fT * ( ( -rkQ ) - rkP );
			}
			else
			{
				result = rkP + fT * ( rkQ - rkP );

			}
			result.Normalize();
			return result;
		}

		/// <summary>
		/// Creates a Quaternion from a supplied angle and axis.
		/// </summary>
		/// <param name="angle">Value of an angle in radians.</param>
		/// <param name="axis">Arbitrary axis vector.</param>
		/// <returns></returns>
		public static Quaternion FromAngleAxis( Real angle, Vector3 axis )
		{
			Quaternion quat = new Quaternion();

			Real halfAngle = 0.5f * angle;
			Real sin = Utility.Sin( halfAngle );

			quat.w = Utility.Cos( halfAngle );
			quat.x = sin * axis.x;
			quat.y = sin * axis.y;
			quat.z = sin * axis.z;

			return quat;
		}

		public static Quaternion Squad( Real t, Quaternion p, Quaternion a, Quaternion b, Quaternion q )
		{
			return Squad( t, p, a, b, q, false );
		}

		/// <summary>
		///		Performs spherical quadratic interpolation.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="p"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="q"></param>
		/// <returns></returns>
		public static Quaternion Squad( Real t, Quaternion p, Quaternion a, Quaternion b, Quaternion q, bool useShortestPath )
		{
			Real slerpT = 2.0f * t * ( 1.0f - t );

			// use spherical linear interpolation
			Quaternion slerpP = Slerp( t, p, q, useShortestPath );
			Quaternion slerpQ = Slerp( t, a, b );

			// run another Slerp on the results of the first 2, and return the results
			return Slerp( slerpT, slerpP, slerpQ );
		}

		#endregion

		#region Public methods

		#region Euler Angles

		public Vector3 ToEulerAnglesInDegrees()
		{
			Real pitch, yaw, roll;
			ToEulerAngles( out pitch, out yaw, out roll );
			return new Vector3( Utility.RadiansToDegrees( pitch ), Utility.RadiansToDegrees( yaw ), Utility.RadiansToDegrees( roll ) );
		}

		public Vector3 ToEulerAngles()
		{
			Real pitch, yaw, roll;
			ToEulerAngles( out pitch, out yaw, out roll );
			return new Vector3( pitch, yaw, roll );
		}

		public void ToEulerAnglesInDegrees( out Real pitch, out Real yaw, out Real roll )
		{
			ToEulerAngles( out pitch, out yaw, out roll );
			pitch = Utility.RadiansToDegrees( pitch );
			yaw = Utility.RadiansToDegrees( yaw );
			roll = Utility.RadiansToDegrees( roll );
		}

		public void ToEulerAngles( out Real pitch, out Real yaw, out Real roll )
		{

			Real halfPi = Utility.PI / 2;
			Real test = x * y + z * w;
			if ( test > 0.499f )
			{ // singularity at north pole
				yaw = 2 * Utility.ATan2( x, w );
				roll = halfPi;
				pitch = 0;
			}
			else if ( test < -0.499f )
			{ // singularity at south pole
				yaw = -2 * Utility.ATan2( x, w );
				roll = -halfPi;
				pitch = 0;
			}
			else
			{
				Real sqx = x * x;
				Real sqy = y * y;
				Real sqz = z * z;
				yaw = Utility.ATan2( 2 * y * w - 2 * x * z, 1 - 2 * sqy - 2 * sqz );
				roll = (Real)Utility.ASin( 2 * test );
				pitch = Utility.ATan2( 2 * x * w - 2 * y * z, 1 - 2 * sqx - 2 * sqz );
			}

			if ( pitch <= Real.Epsilon )
				pitch = 0f;
			if ( yaw <= Real.Epsilon )
				yaw = 0f;
			if ( roll <= Real.Epsilon )
				roll = 0f;
		}

		public static Quaternion FromEulerAnglesInDegrees( Real pitch, Real yaw, Real roll )
		{
			return FromEulerAngles( Utility.DegreesToRadians( pitch ), Utility.DegreesToRadians( yaw ), Utility.DegreesToRadians( roll ) );
		}

		/// <summary>
		/// Combines the euler angles in the order yaw, pitch, roll to create a rotation quaternion
		/// </summary>
		/// <param name="pitch"></param>
		/// <param name="yaw"></param>
		/// <param name="roll"></param>
		/// <returns></returns>
		public static Quaternion FromEulerAngles( Real pitch, Real yaw, Real roll )
		{
			return Quaternion.FromAngleAxis( yaw, Vector3.UnitY )
				* Quaternion.FromAngleAxis( pitch, Vector3.UnitX )
				* Quaternion.FromAngleAxis( roll, Vector3.UnitZ );

			/*TODO: Debug
			//Equation from http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/index.htm
			//heading
			
			Real c1 = (Real)Math.Cos(yaw/2);
			Real s1 = (Real)Math.Sin(yaw/2);
			//attitude
			Real c2 = (Real)Math.Cos(roll/2);
			Real s2 = (Real)Math.Sin(roll/2);
			//bank
			Real c3 = (Real)Math.Cos(pitch/2);
			Real s3 = (Real)Math.Sin(pitch/2);
			Real c1c2 = c1*c2;
			Real s1s2 = s1*s2;

			Real w =c1c2*c3 - s1s2*s3;
			Real x =c1c2*s3 + s1s2*c3;
			Real y =s1*c2*c3 + c1*s2*s3;
			Real z =c1*s2*c3 - s1*c2*s3;
			return new Quaternion(w,x,y,z);*/
		}

		#endregion

		/// <summary>
		/// Performs a Dot Product operation on 2 Quaternions.
		/// </summary>
		/// <param name="quat"></param>
		/// <returns></returns>
		public Real Dot( Quaternion quat )
		{
			return this.w * quat.w + this.x * quat.x + this.y * quat.y + this.z * quat.z;
		}

		/// <summary>
		///		Normalizes elements of this quaterion to the range [0,1].
		/// </summary>
		public void Normalize()
		{
			Real factor = 1.0f / Utility.Sqrt( this.Norm );

			w = w * factor;
			x = x * factor;
			y = y * factor;
			z = z * factor;
		}

		/// <summary>
		///    
		/// </summary>
		/// <param name="angle"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public void ToAngleAxis( ref Real angle, ref Vector3 axis )
		{
			// The quaternion representing the rotation is
			//   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

			Real sqrLength = x * x + y * y + z * z;

			if ( sqrLength > 0.0f )
			{
				angle = 2.0f * (Real)Utility.ACos( w );
				Real invLength = Utility.InvSqrt( sqrLength );
				axis.x = x * invLength;
				axis.y = y * invLength;
				axis.z = z * invLength;
			}
			else
			{
				angle = 0.0f;
				axis.x = 1.0f;
				axis.y = 0.0f;
				axis.z = 0.0f;
			}
		}

		/// <summary>
		/// Gets a 3x3 rotation matrix from this Quaternion.
		/// </summary>
		/// <returns></returns>
		public Matrix3 ToRotationMatrix()
		{
			Matrix3 rotation = new Matrix3();

			Real tx = 2.0f * this.x;
			Real ty = 2.0f * this.y;
			Real tz = 2.0f * this.z;
			Real twx = tx * this.w;
			Real twy = ty * this.w;
			Real twz = tz * this.w;
			Real txx = tx * this.x;
			Real txy = ty * this.x;
			Real txz = tz * this.x;
			Real tyy = ty * this.y;
			Real tyz = tz * this.y;
			Real tzz = tz * this.z;

			rotation.m00 = 1.0f - ( tyy + tzz );
			rotation.m01 = txy - twz;
			rotation.m02 = txz + twy;
			rotation.m10 = txy + twz;
			rotation.m11 = 1.0f - ( txx + tzz );
			rotation.m12 = tyz - twx;
			rotation.m20 = txz - twy;
			rotation.m21 = tyz + twx;
			rotation.m22 = 1.0f - ( txx + tyy );

			return rotation;
		}

		/// <summary>
		/// Computes the inverse of a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion Inverse()
		{
			Real norm = this.w * this.w + this.x * this.x + this.y * this.y + this.z * this.z;
			if ( norm > 0.0f )
			{
				Real inverseNorm = 1.0f / norm;
				return new Quaternion( this.w * inverseNorm, -this.x * inverseNorm, -this.y * inverseNorm, -this.z * inverseNorm );
			}
			else
			{
				// return an invalid result to flag the error
				return Quaternion.Zero;
			}
		}

		/// <summary>
		///   Variant of Inverse() that is only valid for unit quaternions.
		/// </summary>
		/// <returns></returns>
		public Quaternion UnitInverse
		{
			get
			{
				return new Quaternion( w, -x, -y, -z );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xAxis"></param>
		/// <param name="yAxis"></param>
		/// <param name="zAxis"></param>
		public void ToAxes( out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis )
		{
			xAxis = new Vector3();
			yAxis = new Vector3();
			zAxis = new Vector3();

			Matrix3 rotation = this.ToRotationMatrix();

			xAxis.x = rotation.m00;
			xAxis.y = rotation.m10;
			xAxis.z = rotation.m20;

			yAxis.x = rotation.m01;
			yAxis.y = rotation.m11;
			yAxis.z = rotation.m21;

			zAxis.x = rotation.m02;
			zAxis.y = rotation.m12;
			zAxis.z = rotation.m22;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xAxis"></param>
		/// <param name="yAxis"></param>
		/// <param name="zAxis"></param>
		public static Quaternion FromAxes( Vector3 xAxis, Vector3 yAxis, Vector3 zAxis )
		{
			Matrix3 rotation = new Matrix3();

			rotation.m00 = xAxis.x;
			rotation.m10 = xAxis.y;
			rotation.m20 = xAxis.z;

			rotation.m01 = yAxis.x;
			rotation.m11 = yAxis.y;
			rotation.m21 = yAxis.z;

			rotation.m02 = zAxis.x;
			rotation.m12 = zAxis.y;
			rotation.m22 = zAxis.z;

			// set this quaternions values from the rotation matrix built
			return FromRotationMatrix( rotation );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matrix"></param>
		public static Quaternion FromRotationMatrix( Matrix3 matrix )
		{
			// Algorithm in Ken Shoemake's article in 1987 SIGGRAPH course notes
			// article "Quaternion Calculus and Fast Animation".

			Quaternion result = Quaternion.Zero;

			Real trace = matrix.m00 + matrix.m11 + matrix.m22;

			Real root = 0.0f;

			if ( trace > 0.0f )
			{
				// |this.w| > 1/2, may as well choose this.w > 1/2
				root = Utility.Sqrt( trace + 1.0f );  // 2w
				result.w = 0.5f * root;

				root = 0.5f / root;  // 1/(4w)

				result.x = ( matrix.m21 - matrix.m12 ) * root;
				result.y = ( matrix.m02 - matrix.m20 ) * root;
				result.z = ( matrix.m10 - matrix.m01 ) * root;
			}
			else
			{
				// |result.w| <= 1/2

				int i = 0;
				if ( matrix.m11 > matrix.m00 )
					i = 1;
				if ( matrix.m22 > matrix[ i, i ] )
					i = 2;

				int j = next[ i ];
				int k = next[ j ];

				root = Utility.Sqrt( matrix[ i, i ] - matrix[ j, j ] - matrix[ k, k ] + 1.0f );

				unsafe
				{
					Real* apkQuat = &result.x;

					apkQuat[ i ] = 0.5f * root;
					root = 0.5f / root;

					result.w = ( matrix[ k, j ] - matrix[ j, k ] ) * root;

					apkQuat[ j ] = ( matrix[ j, i ] + matrix[ i, j ] ) * root;
					apkQuat[ k ] = ( matrix[ k, i ] + matrix[ i, k ] ) * root;
				}
			}

			return result;
		}

		/// <summary>
		///		Calculates the logarithm of a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion Log()
		{
			// BLACKBOX: Learn this
			// If q = cos(A)+sin(A)*(x*i+y*j+z*k) where (x,y,z) is unit length, then
			// log(q) = A*(x*i+y*j+z*k).  If sin(A) is near zero, use log(q) =
			// sin(A)*(x*i+y*j+z*k) since sin(A)/A has limit 1.

			// start off with a zero quat
			Quaternion result = Quaternion.Zero;

			if ( Utility.Abs( w ) < 1.0f )
			{
				Real angle = (Real)Utility.ACos( w );
				Real sin = Utility.Sin( angle );

				if ( Utility.Abs( sin ) >= EPSILON )
				{
					Real coeff = angle / sin;
					result.x = coeff * x;
					result.y = coeff * y;
					result.z = coeff * z;
				}
				else
				{
					result.x = x;
					result.y = y;
					result.z = z;
				}
			}

			return result;
		}

		/// <summary>
		///		Calculates the Exponent of a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion Exp()
		{
			// If q = A*(x*i+y*j+z*k) where (x,y,z) is unit length, then
			// exp(q) = cos(A)+sin(A)*(x*i+y*j+z*k).  If sin(A) is near zero,
			// use exp(q) = cos(A)+A*(x*i+y*j+z*k) since A/sin(A) has limit 1.

			Real angle = Utility.Sqrt( x * x + y * y + z * z );
			Real sin = Utility.Sin( angle );

			// start off with a zero quat
			Quaternion result = Quaternion.Zero;

			result.w = Utility.Cos( angle );

			if ( Utility.Abs( sin ) >= EPSILON )
			{
				Real coeff = sin / angle;

				result.x = coeff * x;
				result.y = coeff * y;
				result.z = coeff * z;
			}
			else
			{
				result.x = x;
				result.y = y;
				result.z = z;
			}

			return result;
		}

		#endregion

		#region Object overloads

		/// <summary>
		///		Overrides the Object.ToString() method to provide a text representation of 
		///		a Quaternion.
		/// </summary>
		/// <returns>A string representation of a Quaternion.</returns>
		public override string ToString()
		{
			return string.Format( CultureInfo.InvariantCulture, "Quaternion({0}, {1}, {2}, {3})", this.w, this.x, this.y, this.z );
		}

		public override int GetHashCode()
		{
			return (int)x ^ (int)y ^ (int)z ^ (int)w;
		}

		public override bool Equals( object obj )
		{
			Quaternion quat = (Quaternion)obj;

			return quat == this;
		}

		public bool Equals( Quaternion rhs, Real tolerance )
		{
			Real fCos = Dot( rhs );
			Real angle = (Real)Utility.ACos( fCos );

			return Utility.Abs( angle ) <= tolerance;
		}

		#endregion

		#region Parse from string

		public Quaternion Parse( string quat )
		{
			// the format is "Quaternion(w, x, y, z)"
			if ( !quat.StartsWith( "Quaternion(" ) )
				throw new FormatException();

			string[] values = quat.Substring( 11 ).TrimEnd( ')' ).Split( ',' );

			return new Quaternion( Real.Parse( values[ 0 ], CultureInfo.InvariantCulture ),
								  Real.Parse( values[ 1 ], CultureInfo.InvariantCulture ),
								  Real.Parse( values[ 2 ], CultureInfo.InvariantCulture ),
								  Real.Parse( values[ 3 ], CultureInfo.InvariantCulture ) );

		}

		#endregion
	}
}
