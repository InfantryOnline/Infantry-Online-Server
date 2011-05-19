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
//     <id value="$Id: Proclaim.cs 1256 2008-03-21 14:35:53Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Utilities
{
	/// <summary>
	/// 
	/// </summary>
	public static class Proclaim
	{
		/// <summary>
		/// Asserts if this statement is reached.
		/// </summary>
		/// <exception cref="InvalidOperationException">Code is supposed to be unreachable.</exception>
		public static Exception Unreachable
		{
			get
			{
				Debug.Assert( false, "Unreachable" );
				return new InvalidOperationException( "Code is supposed to be unreachable." );
			}
		}

		/// <summary>
		/// Asserts if any argument is <c>null</c>.
		/// </summary>
		/// <param name="vars"></param>
		public static void NotNull( params object[] vars )
		{
			bool result = true;
			foreach ( object obj in vars )
			{
				result &= ( obj != null );
			}
			Debug.Assert( result );
		}

		/// <summary>
		/// Asserts if the string is <c>null</c> or zero length.
		/// </summary>
		/// <param name="str"></param>
		public static void NotEmpty( string str )
		{
			Debug.Assert( !String.IsNullOrEmpty( str ) );
		}

		/// <summary>
		/// Asserts if the collection is <c>null</c> or the <c>Count</c> is zero.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		public static void NotEmpty<T>( ICollection<T> items )
		{
			Debug.Assert( items != null && items.Count > 0 );
		}

		/// <summary>
		/// Asserts if any item in the collection is <c>null</c>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		public static void NotNullItems<T>( IEnumerable<T> items ) where T : class
		{
			Debug.Assert( items != null );
			foreach ( object item in items )
			{
				Debug.Assert( item != null );
			}
		}
	}
}
