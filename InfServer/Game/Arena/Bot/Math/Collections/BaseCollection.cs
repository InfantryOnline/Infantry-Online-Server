#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: BaseCollection.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;

#endregion Namespace Declarations

namespace Axiom.Math.Collections
{
    /// <summary>
    ///		Serves as a basis for strongly typed collections in the math lib.
    /// </summary>
    /// <remarks>
    ///		Can't wait for Generics in .Net Framework 2.0!   
    /// </remarks>
    public abstract class BaseCollection : ICollection, IEnumerable, IEnumerator
    {
        /// <summary></summary>
        protected ArrayList objectList;
        //		protected int nextUniqueKeyCounter;

        const int INITIAL_CAPACITY = 50;

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        public BaseCollection()
        {
            objectList = new ArrayList( INITIAL_CAPACITY );
        }

        #endregion

        /// <summary>
        ///		
        /// </summary>
        public object this[ int index ]
        {
            get
            {
                return objectList[ index ];
            }
            set
            {
                objectList[ index ] = value;
            }
        }

        /// <summary>
        ///		Adds an item to the collection.
        /// </summary>
        /// <param name="item"></param>
        protected void Add( object item )
        {
            objectList.Add( item );
        }

        /// <summary>
        ///		Clears all objects from the collection.
        /// </summary>
        public void Clear()
        {
            objectList.Clear();
        }

        /// <summary>
        ///		Removes the item from the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Remove( object item )
        {
            int index = objectList.IndexOf( item );

            if ( index != -1 )
                objectList.RemoveAt( index );
        }

        #region Implementation of ICollection

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo( System.Array array, int index )
        {
            objectList.CopyTo( array, index );
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return objectList.IsSynchronized;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get
            {
                return objectList.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return objectList.SyncRoot;
            }
        }

        #endregion

        #region Implementation of IEnumerable

        public System.Collections.IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion

        #region Implementation of IEnumerator

        private int position = -1;

        /// <summary>
        ///		Resets the in progress enumerator.
        /// </summary>
        public void Reset()
        {
            // reset the enumerator position
            position = -1;
        }

        /// <summary>
        ///		Moves to the next item in the enumeration if there is one.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            position += 1;

            if ( position >= objectList.Count )
                return false;
            else
                return true;
        }

        /// <summary>
        ///		Returns the current object in the enumeration.
        /// </summary>
        public object Current
        {
            get
            {
                return objectList[ position ];
            }
        }
        #endregion
    }
}
