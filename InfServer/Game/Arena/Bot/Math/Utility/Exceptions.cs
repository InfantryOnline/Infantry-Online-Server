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
//     <id value="$Id: AxiomException.cs 939 2006-12-06 01:39:38Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Utilities
{
	/// <summary>
	/// Factory class for some exception classes that have variable constructors based on the 
	/// framework that is targeted. Rather than use <c>#if</c> around the different constructors
	/// use the least common denominator, but wrap it in an easier to use method.
	/// </summary>
	internal static class ExceptionFactory
	{
		/// <summary>
		/// Factory for the <c>ArgumentOutOfRangeException</c>
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException( string name, object value, string message )
		{
			return new ArgumentOutOfRangeException( name, string.Format( "{0} (actual value is '{1}')", message, value ) );
		}

		/// <summary>
		/// Factory for the <c>ArgumentOutOfRangeException</c>
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static ArgumentNullException CreateArgumentItemNullException( int index, string arrayName )
		{
			return new ArgumentNullException( String.Format( "{0}[{1}]", arrayName, index ) );
		}
	}
}
