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
//     <id value="$Id: QuaternionCollection.cs 2406 2011-02-02 23:02:44Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Math;

#endregion Namespace Declarations

// used to alias a type in the code for easy copying and pasting.  Come on generics!!
using T = Axiom.Math.Quaternion;

namespace Axiom.Math.Collections
{
    /*
    /// <summary>
    /// Summary description for QuaternionCollection.
    /// </summary>
    public class QuaternionCollection : BaseCollection {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public QuaternionCollection() : base() {}

        #endregion

        #region Strongly typed methods and indexers

        /// <summary>
        ///		Get/Set indexer that allows access to the collection by index.
        /// </summary>
        new public T this[int index] {
            get { return (T)base[index]; }
            set { base[index] = value; }
        }

        /// <summary>
        ///		Adds an object to the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) {
            base.Add(item);
        }

        #endregion

    } */

    /// <summary>
    ///		A strongly-typed collection of <see cref="Quaternion"/> objects.
    /// </summary>
#if !( XBOX || XBOX360 || SILVERLIGHT )
    [Serializable]
#endif
	public class QuaternionCollection : ICollection, IList, IEnumerable, ICloneable
    {
        #region Interfaces
        /// <summary>
        ///		Supports type-safe iteration over a <see cref="QuaternionCollection"/>.
        /// </summary>
        public interface IQuaternionCollectionEnumerator
        {
            /// <summary>
            ///		Gets the current element in the collection.
            /// </summary>
            Quaternion Current
            {
                get;
            }

            /// <summary>
            ///		Advances the enumerator to the next element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            ///		The collection was modified after the enumerator was created.
            /// </exception>
            /// <returns>
            ///		<c>true</c> if the enumerator was successfully advanced to the next element; 
            ///		<c>false</c> if the enumerator has passed the end of the collection.
            /// </returns>
            bool MoveNext();

            /// <summary>
            ///		Sets the enumerator to its initial position, before the first element in the collection.
            /// </summary>
            void Reset();
        }
        #endregion

        private const int DEFAULT_CAPACITY = 16;

        #region Implementation (data)
        private Quaternion[] m_array;
        private int m_count = 0;
#if !(XBOX || XBOX360 || SILVERLIGHT)
        [NonSerialized]
#endif
        private int m_version = 0;
        #endregion

        #region Static Wrappers
        /// <summary>
        ///		Creates a synchronized (thread-safe) wrapper for a 
        ///     <c>QuaternionCollection</c> instance.
        /// </summary>
        /// <returns>
        ///     An <c>QuaternionCollection</c> wrapper that is synchronized (thread-safe).
        /// </returns>
        public static QuaternionCollection Synchronized( QuaternionCollection list )
        {
            if ( list == null )
                throw new ArgumentNullException( "list" );
            return new SyncQuaternionCollection( list );
        }

        /// <summary>
        ///		Creates a read-only wrapper for a 
        ///     <c>QuaternionCollection</c> instance.
        /// </summary>
        /// <returns>
        ///     An <c>QuaternionCollection</c> wrapper that is read-only.
        /// </returns>
        public static QuaternionCollection ReadOnly( QuaternionCollection list )
        {
            if ( list == null )
                throw new ArgumentNullException( "list" );
            return new ReadOnlyQuaternionCollection( list );
        }
        #endregion

        #region Construction
        /// <summary>
        ///		Initializes a new instance of the <c>QuaternionCollection</c> class
        ///		that is empty and has the default initial capacity.
        /// </summary>
        public QuaternionCollection()
        {
            m_array = new Quaternion[ DEFAULT_CAPACITY ];
        }

        /// <summary>
        ///		Initializes a new instance of the <c>QuaternionCollection</c> class
        ///		that has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">
        ///		The number of elements that the new <c>QuaternionCollection</c> is initially capable of storing.
        ///	</param>
        public QuaternionCollection( int capacity )
        {
            m_array = new Quaternion[ capacity ];
        }

        /// <summary>
        ///		Initializes a new instance of the <c>QuaternionCollection</c> class
        ///		that contains elements copied from the specified <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="c">The <c>QuaternionCollection</c> whose elements are copied to the new collection.</param>
        public QuaternionCollection( QuaternionCollection c )
        {
            m_array = new Quaternion[ c.Count ];
            AddRange( c );
        }

        /// <summary>
        ///		Initializes a new instance of the <c>QuaternionCollection</c> class
        ///		that contains elements copied from the specified <see cref="Quaternion"/> array.
        /// </summary>
        /// <param name="a">The <see cref="Quaternion"/> array whose elements are copied to the new list.</param>
        public QuaternionCollection( Quaternion[] a )
        {
            m_array = new Quaternion[ a.Length ];
            AddRange( a );
        }

        protected enum Tag
        {
            Default
        }

        protected QuaternionCollection( Tag t )
        {
            m_array = null;
        }
        #endregion

        #region Operations (type-safe ICollection)
        /// <summary>
        ///		Gets the number of elements actually contained in the <c>QuaternionCollection</c>.
        /// </summary>
        public virtual int Count
        {
            get
            {
                return m_count;
            }
        }

        /// <summary>
        ///		Copies the entire <c>QuaternionCollection</c> to a one-dimensional
        ///		<see cref="Quaternion"/> array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Quaternion"/> array to copy to.</param>
        public virtual void CopyTo( Quaternion[] array )
        {
            this.CopyTo( array, 0 );
        }

        /// <summary>
        ///		Copies the entire <c>QuaternionCollection</c> to a one-dimensional
        ///		<see cref="Quaternion"/> array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Quaternion"/> array to copy to.</param>
        /// <param name="start">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public virtual void CopyTo( Quaternion[] array, int start )
        {
            if ( m_count > array.GetUpperBound( 0 ) + 1 - start )
                throw new System.ArgumentException( "Destination array was not long enough." );

            Array.Copy( m_array, 0, array, start, m_count );
        }

        /// <summary>
        ///		Gets a value indicating whether access to the collection is synchronized (thread-safe).
        /// </summary>
        /// <returns>true if access to the ICollection is synchronized (thread-safe); otherwise, false.</returns>
        public virtual bool IsSynchronized
        {
            get
            {
                return m_array.IsSynchronized;
            }
        }

        /// <summary>
        ///		Gets an object that can be used to synchronize access to the collection.
        /// </summary>
        public virtual object SyncRoot
        {
            get
            {
                return m_array.SyncRoot;
            }
        }
        #endregion

        #region Operations (type-safe IList)
        /// <summary>
        ///		Gets or sets the <see cref="Quaternion"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		<para><paramref name="index"/> is less than zero</para>
        ///		<para>-or-</para>
        ///		<para><paramref name="index"/> is equal to or greater than <see cref="QuaternionCollection.Count"/>.</para>
        /// </exception>
        public virtual Quaternion this[ int index ]
        {
            get
            {
                ValidateIndex( index ); // throws
                return m_array[ index ];
            }
            set
            {
                ValidateIndex( index ); // throws
                ++m_version;
                m_array[ index ] = value;
            }
        }

        /// <summary>
        ///		Adds a <see cref="Quaternion"/> to the end of the <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="item">The <see cref="Quaternion"/> to be added to the end of the <c>QuaternionCollection</c>.</param>
        /// <returns>The index at which the value has been added.</returns>
        public virtual int Add( Quaternion item )
        {
            if ( m_count == m_array.Length )
                EnsureCapacity( m_count + 1 );

            m_array[ m_count ] = item;
            m_version++;

            return m_count++;
        }

        /// <summary>
        ///		Removes all elements from the <c>QuaternionCollection</c>.
        /// </summary>
        public virtual void Clear()
        {
            ++m_version;
            m_array = new Quaternion[ DEFAULT_CAPACITY ];
            m_count = 0;
        }

        /// <summary>
        ///		Creates a shallow copy of the <see cref="QuaternionCollection"/>.
        /// </summary>
        public virtual object Clone()
        {
            QuaternionCollection newColl = new QuaternionCollection( m_count );
            Array.Copy( m_array, 0, newColl.m_array, 0, m_count );
            newColl.m_count = m_count;
            newColl.m_version = m_version;

            return newColl;
        }

        /// <summary>
        ///		Determines whether a given <see cref="Quaternion"/> is in the <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="item">The <see cref="Quaternion"/> to check for.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is found in the <c>QuaternionCollection</c>; otherwise, <c>false</c>.</returns>
        public virtual bool Contains( Quaternion item )
        {
            for ( int i = 0; i != m_count; ++i )
                if ( m_array[ i ].Equals( item ) )
                    return true;
            return false;
        }

        /// <summary>
        ///		Returns the zero-based index of the first occurrence of a <see cref="Quaternion"/>
        ///		in the <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="item">The <see cref="Quaternion"/> to locate in the <c>QuaternionCollection</c>.</param>
        /// <returns>
        ///		The zero-based index of the first occurrence of <paramref name="item"/> 
        ///		in the entire <c>QuaternionCollection</c>, if found; otherwise, -1.
        ///	</returns>
        public virtual int IndexOf( Quaternion item )
        {
            for ( int i = 0; i != m_count; ++i )
                if ( m_array[ i ].Equals( item ) )
                    return i;
            return -1;
        }

        /// <summary>
        ///		Inserts an element into the <c>QuaternionCollection</c> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="Quaternion"/> to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		<para><paramref name="index"/> is less than zero</para>
        ///		<para>-or-</para>
        ///		<para><paramref name="index"/> is equal to or greater than <see cref="QuaternionCollection.Count"/>.</para>
        /// </exception>
        public virtual void Insert( int index, Quaternion item )
        {
            ValidateIndex( index, true ); // throws

            if ( m_count == m_array.Length )
                EnsureCapacity( m_count + 1 );

            if ( index < m_count )
            {
                Array.Copy( m_array, index, m_array, index + 1, m_count - index );
            }

            m_array[ index ] = item;
            m_count++;
            m_version++;
        }

        /// <summary>
        ///		Removes the first occurrence of a specific <see cref="Quaternion"/> from the <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="item">The <see cref="Quaternion"/> to remove from the <c>QuaternionCollection</c>.</param>
        /// <exception cref="ArgumentException">
        ///		The specified <see cref="Quaternion"/> was not found in the <c>QuaternionCollection</c>.
        /// </exception>
        public virtual void Remove( Quaternion item )
        {
            int i = IndexOf( item );
            if ( i < 0 )
                throw new System.ArgumentException( "Cannot remove the specified item because it was not found in the specified Collection." );

            ++m_version;
            RemoveAt( i );
        }

        /// <summary>
        ///		Removes the element at the specified index of the <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		<para><paramref name="index"/> is less than zero</para>
        ///		<para>-or-</para>
        ///		<para><paramref name="index"/> is equal to or greater than <see cref="QuaternionCollection.Count"/>.</para>
        /// </exception>
        public virtual void RemoveAt( int index )
        {
            ValidateIndex( index ); // throws

            m_count--;

            if ( index < m_count )
            {
                Array.Copy( m_array, index + 1, m_array, index, m_count - index );
            }

            // We can't set the deleted entry equal to null, because it might be a value type.
            // Instead, we'll create an empty single-element array of the right type and copy it 
            // over the entry we want to erase.
            Quaternion[] temp = new Quaternion[ 1 ];
            Array.Copy( temp, 0, m_array, m_count, 1 );
            m_version++;
        }

        /// <summary>
        ///		Gets a value indicating whether the collection has a fixed size.
        /// </summary>
        /// <value>true if the collection has a fixed size; otherwise, false. The default is false</value>
        public virtual bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///		gets a value indicating whether the <B>IList</B> is read-only.
        /// </summary>
        /// <value>true if the collection is read-only; otherwise, false. The default is false</value>
        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        #endregion

        #region Operations (type-safe IEnumerable)

        /// <summary>
        ///		Returns an enumerator that can iterate through the <c>QuaternionCollection</c>.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> for the entire <c>QuaternionCollection</c>.</returns>
        public virtual IQuaternionCollectionEnumerator GetEnumerator()
        {
            return new Enumerator( this );
        }
        #endregion

        #region Public helpers (just to mimic some nice features of ArrayList)

        /// <summary>
        ///		Gets or sets the number of elements the <c>QuaternionCollection</c> can contain.
        /// </summary>
        public virtual int Capacity
        {
            get
            {
                return m_array.Length;
            }

            set
            {
                if ( value < m_count )
                    value = m_count;

                if ( value != m_array.Length )
                {
                    if ( value > 0 )
                    {
                        Quaternion[] temp = new Quaternion[ value ];
                        Array.Copy( m_array, temp, m_count );
                        m_array = temp;
                    }
                    else
                    {
                        m_array = new Quaternion[ DEFAULT_CAPACITY ];
                    }
                }
            }
        }

        /// <summary>
        ///		Adds the elements of another <c>QuaternionCollection</c> to the current <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="x">The <c>QuaternionCollection</c> whose elements should be added to the end of the current <c>QuaternionCollection</c>.</param>
        /// <returns>The new <see cref="QuaternionCollection.Count"/> of the <c>QuaternionCollection</c>.</returns>
        public virtual int AddRange( QuaternionCollection x )
        {
            if ( m_count + x.Count >= m_array.Length )
                EnsureCapacity( m_count + x.Count );

            Array.Copy( x.m_array, 0, m_array, m_count, x.Count );
            m_count += x.Count;
            m_version++;

            return m_count;
        }

        /// <summary>
        ///		Adds the elements of a <see cref="Quaternion"/> array to the current <c>QuaternionCollection</c>.
        /// </summary>
        /// <param name="x">The <see cref="Quaternion"/> array whose elements should be added to the end of the <c>QuaternionCollection</c>.</param>
        /// <returns>The new <see cref="QuaternionCollection.Count"/> of the <c>QuaternionCollection</c>.</returns>
        public virtual int AddRange( Quaternion[] x )
        {
            if ( m_count + x.Length >= m_array.Length )
                EnsureCapacity( m_count + x.Length );

            Array.Copy( x, 0, m_array, m_count, x.Length );
            m_count += x.Length;
            m_version++;

            return m_count;
        }

        /// <summary>
        ///		Sets the capacity to the actual number of elements.
        /// </summary>
        public virtual void TrimToSize()
        {
            this.Capacity = m_count;
        }

        #endregion

        #region Implementation (helpers)

        /// <exception cref="ArgumentOutOfRangeException">
        ///		<para><paramref name="index"/> is less than zero</para>
        ///		<para>-or-</para>
        ///		<para><paramref name="index"/> is equal to or greater than <see cref="QuaternionCollection.Count"/>.</para>
        /// </exception>
        private void ValidateIndex( int i )
        {
            ValidateIndex( i, false );
        }

        /// <exception cref="ArgumentOutOfRangeException">
        ///		<para><paramref name="index"/> is less than zero</para>
        ///		<para>-or-</para>
        ///		<para><paramref name="index"/> is equal to or greater than <see cref="QuaternionCollection.Count"/>.</para>
        /// </exception>
        private void ValidateIndex( int i, bool allowEqualEnd )
        {
            int max = ( allowEqualEnd ) ? ( m_count ) : ( m_count - 1 );
            if ( i < 0 || i > max )
#if !(XBOX || XBOX360 || SILVERLIGHT )
                throw new System.ArgumentOutOfRangeException( "Index was out of range.  Must be non-negative and less than the size of the collection.", (object)i, "Specified argument was out of the range of valid values." );
#else
                throw new System.ArgumentOutOfRangeException("Index was out of range.  Must be non-negative and less than the size of the collection.", "Specified argument was out of the range of valid values.");
#endif
        }

        private void EnsureCapacity( int min )
        {
            int newCapacity = ( ( m_array.Length == 0 ) ? DEFAULT_CAPACITY : m_array.Length * 2 );
            if ( newCapacity < min )
                newCapacity = min;

            this.Capacity = newCapacity;
        }

        #endregion

        #region Implementation (ICollection)

        void ICollection.CopyTo( Array array, int start )
        {
            Array.Copy( m_array, 0, array, start, m_count );
        }

        #endregion

        #region Implementation (IList)

        object IList.this[ int i ]
        {
            get
            {
                return (object)this[ i ];
            }
            set
            {
                this[ i ] = (Quaternion)value;
            }
        }

        int IList.Add( object x )
        {
            return this.Add( (Quaternion)x );
        }

        bool IList.Contains( object x )
        {
            return this.Contains( (Quaternion)x );
        }

        int IList.IndexOf( object x )
        {
            return this.IndexOf( (Quaternion)x );
        }

        void IList.Insert( int pos, object x )
        {
            this.Insert( pos, (Quaternion)x );
        }

        void IList.Remove( object x )
        {
            this.Remove( (Quaternion)x );
        }

        void IList.RemoveAt( int pos )
        {
            this.RemoveAt( pos );
        }

        #endregion

        #region Implementation (IEnumerable)

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)( this.GetEnumerator() );
        }

        #endregion

        #region Nested enumerator class
        /// <summary>
        ///		Supports simple iteration over a <see cref="QuaternionCollection"/>.
        /// </summary>
        private class Enumerator : IEnumerator, IQuaternionCollectionEnumerator
        {
            #region Implementation (data)

            private QuaternionCollection m_collection;
            private int m_index;
            private int m_version;

            #endregion

            #region Construction

            /// <summary>
            ///		Initializes a new instance of the <c>Enumerator</c> class.
            /// </summary>
            /// <param name="tc"></param>
            internal Enumerator( QuaternionCollection tc )
            {
                m_collection = tc;
                m_index = -1;
                m_version = tc.m_version;
            }

            #endregion

            #region Operations (type-safe IEnumerator)

            /// <summary>
            ///		Gets the current element in the collection.
            /// </summary>
            public Quaternion Current
            {
                get
                {
                    return m_collection[ m_index ];
                }
            }

            /// <summary>
            ///		Advances the enumerator to the next element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            ///		The collection was modified after the enumerator was created.
            /// </exception>
            /// <returns>
            ///		<c>true</c> if the enumerator was successfully advanced to the next element; 
            ///		<c>false</c> if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                if ( m_version != m_collection.m_version )
                    throw new System.InvalidOperationException( "Collection was modified; enumeration operation may not execute." );

                ++m_index;
                return ( m_index < m_collection.Count ) ? true : false;
            }

            /// <summary>
            ///		Sets the enumerator to its initial position, before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                m_index = -1;
            }
            #endregion

            #region Implementation (IEnumerator)

            object IEnumerator.Current
            {
                get
                {
                    return (object)( this.Current );
                }
            }

            #endregion
        }
        #endregion

        #region Nested Syncronized Wrapper class
        private class SyncQuaternionCollection : QuaternionCollection
        {
            #region Implementation (data)
            private QuaternionCollection m_collection;
            private object m_root;
            #endregion

            #region Construction
            internal SyncQuaternionCollection( QuaternionCollection list )
                : base( Tag.Default )
            {
                m_root = list.SyncRoot;
                m_collection = list;
            }
            #endregion

            #region Type-safe ICollection
            public override void CopyTo( Quaternion[] array )
            {
                lock ( this.m_root )
                    m_collection.CopyTo( array );
            }

            public override void CopyTo( Quaternion[] array, int start )
            {
                lock ( this.m_root )
                    m_collection.CopyTo( array, start );
            }
            public override int Count
            {
                get
                {
                    lock ( this.m_root )
                        return m_collection.Count;
                }
            }

            public override bool IsSynchronized
            {
                get
                {
                    return true;
                }
            }

            public override object SyncRoot
            {
                get
                {
                    return this.m_root;
                }
            }
            #endregion

            #region Type-safe IList
            public override Quaternion this[ int i ]
            {
                get
                {
                    lock ( this.m_root )
                        return m_collection[ i ];
                }
                set
                {
                    lock ( this.m_root )
                        m_collection[ i ] = value;
                }
            }

            public override int Add( Quaternion x )
            {
                lock ( this.m_root )
                    return m_collection.Add( x );
            }

            public override void Clear()
            {
                lock ( this.m_root )
                    m_collection.Clear();
            }

            public override bool Contains( Quaternion x )
            {
                lock ( this.m_root )
                    return m_collection.Contains( x );
            }

            public override int IndexOf( Quaternion x )
            {
                lock ( this.m_root )
                    return m_collection.IndexOf( x );
            }

            public override void Insert( int pos, Quaternion x )
            {
                lock ( this.m_root )
                    m_collection.Insert( pos, x );
            }

            public override void Remove( Quaternion x )
            {
                lock ( this.m_root )
                    m_collection.Remove( x );
            }

            public override void RemoveAt( int pos )
            {
                lock ( this.m_root )
                    m_collection.RemoveAt( pos );
            }

            public override bool IsFixedSize
            {
                get
                {
                    return m_collection.IsFixedSize;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return m_collection.IsReadOnly;
                }
            }
            #endregion

            #region Type-safe IEnumerable
            public override IQuaternionCollectionEnumerator GetEnumerator()
            {
                lock ( m_root )
                    return m_collection.GetEnumerator();
            }
            #endregion

            #region Public Helpers
            // (just to mimic some nice features of ArrayList)
            public override int Capacity
            {
                get
                {
                    lock ( this.m_root )
                        return m_collection.Capacity;
                }

                set
                {
                    lock ( this.m_root )
                        m_collection.Capacity = value;
                }
            }

            public override int AddRange( QuaternionCollection x )
            {
                lock ( this.m_root )
                    return m_collection.AddRange( x );
            }

            public override int AddRange( Quaternion[] x )
            {
                lock ( this.m_root )
                    return m_collection.AddRange( x );
            }
            #endregion
        }
        #endregion

        #region Nested Read Only Wrapper class
        private class ReadOnlyQuaternionCollection : QuaternionCollection
        {
            #region Implementation (data)
            private QuaternionCollection m_collection;
            #endregion

            #region Construction
            internal ReadOnlyQuaternionCollection( QuaternionCollection list )
                : base( Tag.Default )
            {
                m_collection = list;
            }
            #endregion

            #region Type-safe ICollection
            public override void CopyTo( Quaternion[] array )
            {
                m_collection.CopyTo( array );
            }

            public override void CopyTo( Quaternion[] array, int start )
            {
                m_collection.CopyTo( array, start );
            }
            public override int Count
            {
                get
                {
                    return m_collection.Count;
                }
            }

            public override bool IsSynchronized
            {
                get
                {
                    return m_collection.IsSynchronized;
                }
            }

            public override object SyncRoot
            {
                get
                {
                    return this.m_collection.SyncRoot;
                }
            }
            #endregion

            #region Type-safe IList
            public override Quaternion this[ int i ]
            {
                get
                {
                    return m_collection[ i ];
                }
                set
                {
                    throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
                }
            }

            public override int Add( Quaternion x )
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }

            public override void Clear()
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }

            public override bool Contains( Quaternion x )
            {
                return m_collection.Contains( x );
            }

            public override int IndexOf( Quaternion x )
            {
                return m_collection.IndexOf( x );
            }

            public override void Insert( int pos, Quaternion x )
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }

            public override void Remove( Quaternion x )
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }

            public override void RemoveAt( int pos )
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }

            public override bool IsFixedSize
            {
                get
                {
                    return true;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }
            #endregion

            #region Type-safe IEnumerable
            public override IQuaternionCollectionEnumerator GetEnumerator()
            {
                return m_collection.GetEnumerator();
            }
            #endregion

            #region Public Helpers
            // (just to mimic some nice features of ArrayList)
            public override int Capacity
            {
                get
                {
                    return m_collection.Capacity;
                }

                set
                {
                    throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
                }
            }

            public override int AddRange( QuaternionCollection x )
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }

            public override int AddRange( Quaternion[] x )
            {
                throw new NotSupportedException( "This is a Read Only Collection and can not be modified" );
            }
            #endregion
        }
        #endregion
    }


}
