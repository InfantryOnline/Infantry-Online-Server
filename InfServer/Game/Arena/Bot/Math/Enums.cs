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
//     <id value="$Id: Enums.cs 2432 2011-02-28 13:48:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	///    Type of intersection detected between 2 object.
	/// </summary>
	public enum Intersection
	{
		/// <summary>
		///    The objects are not intersecting.
		/// </summary>
		None,
		/// <summary>
		///    An object is fully contained within another object.
		/// </summary>
		Contained,
		/// <summary>
		///    An object fully contains another object.
		/// </summary>
		Contains,
		/// <summary>
		///    The objects are partially intersecting each other.
		/// </summary>
		Partial
	}

	/// <summary>
	/// The "positive side" of the plane is the half space to which the
	/// plane normal Points. The "negative side" is the other half
	/// space. The flag "no side" indicates the plane itself.
	/// </summary>
	public enum PlaneSide
	{
		None,
		Positive,
		Negative,
		Both
	}
}
