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
//     <id value="$Id: AxisAlignedBox.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	///		A 3D box aligned with the x/y/z axes.
	/// </summary>
	/// <remarks>
	///		This class represents a simple box which is aligned with the
	///	    axes. It stores 2 points as the extremeties of
	///	    the box, one which is the minima of all 3 axes, and the other
	///	    which is the maxima of all 3 axes. This class is typically used
	///	    for an axis-aligned bounding box (AABB) for collision and
	///	    visibility determination.
	/// </remarks>
	public sealed class AxisAlignedBox : ICloneable
	{
		#region Fields

		internal Vector3 minVector;
		internal Vector3 maxVector;
		private Vector3[] corners = new Vector3[ 8 ];
		private bool isNull;
		private bool isInfinite;

		#endregion

		#region Constructors

		public AxisAlignedBox()
			: this( new Vector3( -0.5f, -0.5f, -0.5f ), new Vector3( 0.5f, 0.5f, 0.5f ) )
		{
			isNull = true;
			isInfinite = false;
		}

		public AxisAlignedBox( Vector3 min, Vector3 max )
		{
			SetExtents( min, max );
		}

		public AxisAlignedBox( AxisAlignedBox box )
		{
			SetExtents( box.Minimum, box.Maximum );
			isNull = box.IsNull;
			isInfinite = box.IsInfinite;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matrix"></param>
		public void Transform( Matrix4 matrix )
		{
			// do nothing for a null box
			if ( isNull || isInfinite )
				return;

			Vector3 min;
			Vector3 max;
			Vector3 temp;

			temp = matrix * corners[ 0 ];
			min = max = temp;

			for ( int i = 1; i < corners.Length; i++ )
			{
				// Transform and check extents
				temp = matrix * corners[ i ];

				if ( temp.x > max.x )
					max.x = temp.x;
				else if ( temp.x < min.x )
					min.x = temp.x;

				if ( temp.y > max.y )
					max.y = temp.y;
				else if ( temp.y < min.y )
					min.y = temp.y;

				if ( temp.z > max.z )
					max.z = temp.z;
				else if ( temp.z < min.z )
					min.z = temp.z;
			}

			SetExtents( min, max );
		}

		/// <summary>
		/// 
		/// </summary>
		private void UpdateCorners()
		{
			// The order of these items is, using right-handed co-ordinates:
			// Minimum Z face, starting with Min(all), then anticlockwise
			//   around face (looking onto the face)
			// Maximum Z face, starting with Max(all), then anticlockwise
			//   around face (looking onto the face)				
			corners[ 0 ] = minVector;
			corners[ 1 ].x = minVector.x;
			corners[ 1 ].y = maxVector.y;
			corners[ 1 ].z = minVector.z;
			corners[ 2 ].x = maxVector.x;
			corners[ 2 ].y = maxVector.y;
			corners[ 2 ].z = minVector.z;
			corners[ 3 ].x = maxVector.x;
			corners[ 3 ].y = minVector.y;
			corners[ 3 ].z = minVector.z;

			corners[ 4 ] = maxVector;
			corners[ 5 ].x = minVector.x;
			corners[ 5 ].y = maxVector.y;
			corners[ 5 ].z = maxVector.z;
			corners[ 6 ].x = minVector.x;
			corners[ 6 ].y = minVector.y;
			corners[ 6 ].z = maxVector.z;
			corners[ 7 ].x = maxVector.x;
			corners[ 7 ].y = minVector.y;
			corners[ 7 ].z = maxVector.z;
		}

		/// <summary>
		///		Sets both Minimum and Maximum at once, so that UpdateCorners only
		///		needs to be called once as well.
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public void SetExtents( Vector3 min, Vector3 max )
		{
			isNull = false;
			isInfinite = false;

			minVector = min;
			maxVector = max;

			UpdateCorners();
		}

		/// <summary>
		///    Scales the size of the box by the supplied factor.
		/// </summary>
		/// <param name="factor">Factor of scaling to apply to the box.</param>
		public void Scale( Vector3 factor )
		{
			SetExtents( minVector * factor, maxVector * factor );
		}

		/// <summary>
		///     Return new bounding box from the supplied dimensions.
		/// </summary>
		/// <param name="center">Center of the new box</param>
		/// <param name="size">Entire size of the new box</param>
		/// <returns>New bounding box</returns>
		public static AxisAlignedBox FromDimensions( Vector3 center, Vector3 size )
		{
			Vector3 halfSize = .5f * size;

			return new AxisAlignedBox( center - halfSize, center + halfSize );
		}


		/// <summary>
		///		Allows for merging two boxes together (combining).
		/// </summary>
		/// <param name="box">Source box.</param>
		public void Merge( AxisAlignedBox box )
		{
			if ( box.IsNull )
			{
				// nothing to merge with in this case, just return
				return;
			}
			else if ( box.IsInfinite )
			{
				this.IsInfinite = true;
			}
			else if ( this.IsNull )
			{
				SetExtents( box.Minimum, box.Maximum );
			}
			else if ( !this.IsInfinite )
			{
				minVector.Floor( box.Minimum );
				maxVector.Ceil( box.Maximum );

				UpdateCorners();
			}
		}

		/// <summary>
		///		Extends the box to encompass the specified point (if needed).
		/// </summary>
		/// <param name="point"></param>
		public void Merge( Vector3 point )
		{
			if ( isNull || isInfinite )
			{
				// if null, use this point
				SetExtents( point, point );
			}
			else
			{
				if ( point.x > maxVector.x )
					maxVector.x = point.x;
				else if ( point.x < minVector.x )
					minVector.x = point.x;

				if ( point.y > maxVector.y )
					maxVector.y = point.y;
				else if ( point.y < minVector.y )
					minVector.y = point.y;

				if ( point.z > maxVector.z )
					maxVector.z = point.z;
				else if ( point.z < minVector.z )
					minVector.z = point.z;

				UpdateCorners();
			}
		}

		#endregion

		#region Contain methods

		/// <summary>
		/// Tests whether the given point contained by this box.
		/// </summary>
		/// <param name="v"></param>
		/// <returns>True if the vector is contained inside the box.</returns>
		public bool Contains( Vector3 v )
		{
			if ( IsNull )
				return false;
			if ( IsInfinite )
				return true;

			return Minimum.x <= v.x && v.x <= Maximum.x &&
				   Minimum.y <= v.y && v.y <= Maximum.y &&
				   Minimum.z <= v.z && v.z <= Maximum.z;
		}


		#endregion Contain methods

		#region Intersection Methods

		/// <summary>
		///		Returns whether or not this box intersects another.
		/// </summary>
		/// <param name="box2"></param>
		/// <returns>True if the 2 boxes intersect, false otherwise.</returns>
		public bool Intersects( AxisAlignedBox box2 )
		{
			// Early-fail for nulls
			if ( this.IsNull || box2.IsNull )
				return false;

			if ( this.IsInfinite || box2.IsInfinite )
				return true;

			// Use up to 6 separating planes
			if ( this.maxVector.x < box2.minVector.x )
				return false;
			if ( this.maxVector.y < box2.minVector.y )
				return false;
			if ( this.maxVector.z < box2.minVector.z )
				return false;

			if ( this.minVector.x > box2.maxVector.x )
				return false;
			if ( this.minVector.y > box2.maxVector.y )
				return false;
			if ( this.minVector.z > box2.maxVector.z )
				return false;

			// otherwise, must be intersecting
			return true;
		}

		/// <summary>
		///		Tests whether this box intersects a sphere.
		/// </summary>
		/// <param name="sphere"></param>
		/// <returns>True if the sphere intersects, false otherwise.</returns>
		public bool Intersects( Sphere sphere )
		{
			return Utility.Intersects( sphere, this );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="plane"></param>
		/// <returns>True if the plane intersects, false otherwise.</returns>
		public bool Intersects( Plane plane )
		{
			return Utility.Intersects( plane, this );
		}

		/// <summary>
		///		Tests whether the vector point is within this box.
		/// </summary>
		/// <param name="vector"></param>
		/// <returns>True if the vector is within this box, false otherwise.</returns>
		public bool Intersects( Vector3 vector )
		{
			return ( vector.x >= minVector.x && vector.x <= maxVector.x &&
				vector.y >= minVector.y && vector.y <= maxVector.y &&
				vector.z >= minVector.z && vector.z <= maxVector.z );
		}

		/// <summary>
		///		Calculate the area of intersection of this box and another
		/// </summary>
		public AxisAlignedBox Intersection( AxisAlignedBox b2 )
		{
			if ( !Intersects( b2 ) )
				return new AxisAlignedBox();

			Vector3 intMin = Vector3.Zero;
			Vector3 intMax = Vector3.Zero;

			Vector3 b2max = b2.maxVector;
			Vector3 b2min = b2.minVector;

			if ( b2max.x > maxVector.x && maxVector.x > b2min.x )
				intMax.x = maxVector.x;
			else
				intMax.x = b2max.x;
			if ( b2max.y > maxVector.y && maxVector.y > b2min.y )
				intMax.y = maxVector.y;
			else
				intMax.y = b2max.y;
			if ( b2max.z > maxVector.z && maxVector.z > b2min.z )
				intMax.z = maxVector.z;
			else
				intMax.z = b2max.z;

			if ( b2min.x < minVector.x && minVector.x < b2max.x )
				intMin.x = minVector.x;
			else
				intMin.x = b2min.x;
			if ( b2min.y < minVector.y && minVector.y < b2max.y )
				intMin.y = minVector.y;
			else
				intMin.y = b2min.y;
			if ( b2min.z < minVector.z && minVector.z < b2max.z )
				intMin.z = minVector.z;
			else
				intMin.z = b2min.z;

			return new AxisAlignedBox( intMin, intMax );
		}

		#endregion Intersection Methods

		#region Properties

		public Vector3 HalfSize
		{
			get
			{
				if ( isNull )
					return Vector3.Zero;

				if ( isInfinite )
					return Vector3.PositiveInfinity;

				return ( Maximum - Minimum ) * 0.5f;
			}
		}

		/// <summary>
		///     Get/set the size of this bounding box.
		/// </summary>
		public Vector3 Size
		{
			get
			{
				return maxVector - minVector;
			}
			set
			{
				Vector3 center = Center;
				Vector3 halfSize = .5f * value;
				minVector = center - halfSize;
				maxVector = center + halfSize;
				UpdateCorners();
			}
		}

		/// <summary>
		///    Get/set the center point of this bounding box.
		/// </summary>
		public Vector3 Center
		{
			get
			{
				return ( minVector + maxVector ) * 0.5f;
			}
			set
			{
				Vector3 halfSize = .5f * Size;
				minVector = value - halfSize;
				maxVector = value + halfSize;
				UpdateCorners();
			}

		}

		/// <summary>
		///		Get/set the maximum corner of the box.
		/// </summary>
		public Vector3 Maximum
		{
			get
			{
				return maxVector;
			}
			set
			{
				isNull = false;
				maxVector = value;
				UpdateCorners();
			}
		}

		/// <summary>
		///		Get/set the minimum corner of the box.
		/// </summary>
		public Vector3 Minimum
		{
			get
			{
				return minVector;
			}
			set
			{
				isNull = false;
				minVector = value;
				UpdateCorners();
			}
		}

		/// <summary>
		///		Returns an array of 8 corner points, useful for
		///		collision vs. non-aligned objects.
		/// </summary>
		/// <remarks>
		///		If the order of these corners is important, they are as
		///		follows: The 4 points of the minimum Z face (note that
		///		because we use right-handed coordinates, the minimum Z is
		///		at the 'back' of the box) starting with the minimum point of
		///		all, then anticlockwise around this face (if you are looking
		///		onto the face from outside the box). Then the 4 points of the
		///		maximum Z face, starting with maximum point of all, then
		///		anticlockwise around this face (looking onto the face from
		///		outside the box). Like this:
		///		<pre>
		///			 1-----2
		///		    /|     /|
		///		  /  |   /  |
		///		5-----4   |
		///		|   0-|--3
		///		|  /   |  /
		///		|/     |/
		///		6-----7
		///		</pre>
		/// </remarks>
		public Vector3[] Corners
		{
			get
			{
				Debug.Assert( !isNull && !isInfinite, "Cannot get the corners of a null or infinite box." );

				return corners;
			}
		}

		/// <summary>
		///		Get/set the value of whether this box is null (i.e. not dimensions, etc).
		/// </summary>
		public bool IsNull
		{
			get
			{
				return isNull;
			}
			set
			{
				isNull = value;
				if ( value )
					isInfinite = false;
			}
		}

		/// <summary>
		/// Returns true if the box is infinite.
		/// </summary>
		public bool IsInfinite
		{
			get
			{
				return isInfinite;
			}
			set
			{
				isInfinite = value;
				if ( value )
					isNull = false;
			}
		}


		/// <summary>
		///		Returns a null box
		/// </summary>
		public static AxisAlignedBox Null
		{
			get
			{
				AxisAlignedBox nullBox = new AxisAlignedBox();
				// make sure it is set to null
				nullBox.IsNull = true;
				nullBox.isInfinite = false;
				return nullBox;
			}
		}

		/// <summary>
		///     Calculate the volume of this box
		/// </summary>
		public Real Volume
		{
			get
			{
				if ( isNull )
					return 0.0f;

				if ( isInfinite )
					return Real.PositiveInfinity;

				Vector3 diff = Maximum - Minimum;
				return diff.x * diff.y * diff.z;
			}
		}


		#endregion

		#region Operator Overloads

		public static bool operator ==( AxisAlignedBox left, AxisAlignedBox right )
		{
			if ( ( object.ReferenceEquals( left, null ) || left.isNull ) &&
				( object.ReferenceEquals( right, null ) || right.isNull ) )
				return true;

			else if ( ( object.ReferenceEquals( left, null ) || left.isNull ) ||
					 ( object.ReferenceEquals( right, null ) || right.isNull ) )
				return false;

			return
				( left.corners[ 0 ] == right.corners[ 0 ] && left.corners[ 1 ] == right.corners[ 1 ] && left.corners[ 2 ] == right.corners[ 2 ] &&
				left.corners[ 3 ] == right.corners[ 3 ] && left.corners[ 4 ] == right.corners[ 4 ] && left.corners[ 5 ] == right.corners[ 5 ] &&
				left.corners[ 6 ] == right.corners[ 6 ] && left.corners[ 7 ] == right.corners[ 7 ] );
		}

		public static bool operator !=( AxisAlignedBox left, AxisAlignedBox right )
		{
			if ( ( object.ReferenceEquals( left, null ) || left.isNull ) &&
				( object.ReferenceEquals( right, null ) || right.isNull ) )
				return false;

			else if ( ( object.ReferenceEquals( left, null ) || left.isNull ) ||
					 ( object.ReferenceEquals( right, null ) || right.isNull ) )
				return true;

			return
				( left.corners[ 0 ] != right.corners[ 0 ] || left.corners[ 1 ] != right.corners[ 1 ] || left.corners[ 2 ] != right.corners[ 2 ] ||
				left.corners[ 3 ] != right.corners[ 3 ] || left.corners[ 4 ] != right.corners[ 4 ] || left.corners[ 5 ] != right.corners[ 5 ] ||
				left.corners[ 6 ] != right.corners[ 6 ] || left.corners[ 7 ] != right.corners[ 7 ] );
		}

		public override bool Equals( object obj )
		{
			return obj is AxisAlignedBox && this == (AxisAlignedBox)obj;
		}

		public override int GetHashCode()
		{
			if ( isNull )
				return 0;

			return corners[ 0 ].GetHashCode() ^ corners[ 1 ].GetHashCode() ^ corners[ 2 ].GetHashCode() ^ corners[ 3 ].GetHashCode() ^ corners[ 4 ].GetHashCode() ^
				corners[ 5 ].GetHashCode() ^ corners[ 6 ].GetHashCode() ^ corners[ 7 ].GetHashCode();
		}

		public override string ToString()
		{
			return String.Format( "{0}:{1}", this.minVector, this.maxVector );
		}

		#endregion

		#region ICloneable Members

		public object Clone()
		{
			return new AxisAlignedBox( this );
		}

		#endregion
	}
}
