using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;

namespace InfServer.Game
{
    /// <summary>
    /// Implements a spatial lookup data structure for players in an arena
    /// </summary>
    public class ObjTracker<T> : ICollection<T>, IEnumerable<T>
        where T : ILocatable
    {
        // These were set based on infantry values
        private const int TICK_MAX = Int16.MaxValue;
        private const int EXACT_TICKS = 16;
        private const int COORD_TICKS = EXACT_TICKS * 80; // 1280

        // Each bucket will be half a coord
        private const int BUCKET_TICKS = COORD_TICKS / 2; // 640
        private const int BUCKET_COUNT = TICK_MAX / BUCKET_TICKS + 1; // 52

        // Use a dictionary to easily look up the last bucket a player was in
        private Dictionary<T, List<T>> _objToBucket;
        private List<T>[,] _matrix;

        private Dictionary<ushort, T> _idToObj;

        // Our default predicate for retrieving objects
        private Func<T, bool> _defPredicate;

        /// <summary>
        /// Generic Constructor
        /// </summary>
        public ObjTracker()
        {
            _defPredicate = null;

            resetStructures();
        }

        /// <summary>
        /// Generic Constructor
        /// </summary>
        public ObjTracker(Func<T, bool> defaultPredicate)
        {
            _defPredicate = defaultPredicate;

            resetStructures();
        }

        /// <summary>
        /// Initializes data structures. Called by the constructor and Clear()
        /// </summary>
        private void resetStructures()
        {
            _objToBucket = new Dictionary<T, List<T>>();

            _idToObj = new Dictionary<ushort, T>();

            // Initialize buckets
            _matrix = new List<T>[BUCKET_COUNT, BUCKET_COUNT];

            for (int i = 0; i < BUCKET_COUNT; i++)
            {
                for (int j = 0; j < BUCKET_COUNT; j++)
                {
                    _matrix[i, j] = new List<T>();
                }
            }
        }

        public void updateObjState(T from, Helpers.ObjectState state)
        {	//Make sure he's one of ours
            try
            {
                if (!Contains(from))
                {
                    Log.write(TLog.Warning, "Given object state update for unknown object {0}.", from);
                    return;
                }
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, String.Format("{0} Details = {1}, {2}", e.ToString(), _idToObj.Count(), from.getID()));
            }

            // Update the bucket if it's not correct
            List<T> newBucket = _matrix[state.positionX / BUCKET_TICKS, state.positionY / BUCKET_TICKS];
            List<T> oldBucket;

            if (!_objToBucket.TryGetValue(from, out oldBucket))
                _objToBucket[from] = newBucket;
            else if (oldBucket != newBucket)
            {	// Move buckets
                oldBucket.Remove(from);
                newBucket.Add(from);

                _objToBucket[from] = newBucket;
            }
        }

        public T getObjByID(ushort id)
        {
            T p = default(T);
            if (!_idToObj.TryGetValue(id, out p))
                return default(T);
            return p;
        }

        /// <summary>
        /// Returns objects in the circle of radius range centered at xPos, yPos
        /// </summary>		
        public List<T> getObjsInRange(int xPos, int yPos, int range)
        {
            return getObjsByClosure(xPos - range, yPos - range, xPos + range, yPos + range, delegate(T p)
            {	//Does it satisfy the predicate?
                if ((_defPredicate != null && !_defPredicate(p)))
                    return false;

                Helpers.ObjectState state = p.getState();
                int px = state.positionX;
                int py = state.positionY;

                return (Math.Pow(px - xPos, 2) + Math.Pow(py - yPos, 2)) < (Math.Pow(range, 2));
            });
        }

        /// <summary>
        /// Returns objects in the circle of radius range centered at xPos, yPos
        /// </summary>		
        public List<T> getObjsInRange(int xPos, int yPos, int range, Predicate<T> predicate)
        {
            return getObjsByClosure(xPos - range, yPos - range, xPos + range, yPos + range, delegate(T p)
            {	//Does it satisfy the predicate?
                if ((_defPredicate != null && !_defPredicate(p)) || !predicate(p))
                    return false;

                Helpers.ObjectState state = p.getState();
                int px = state.positionX;
                int py = state.positionY;

                return (Math.Pow(px - xPos, 2) + Math.Pow(py - yPos, 2)) < (Math.Pow(range, 2));
            });
        }

        /// <summary>
        /// Returns objects inside the box defined by the parameters
        /// </summary>		
        public List<T> getObjsInArea(int xMin, int yMin, int xMax, int yMax)
        {
            return getObjsByClosure(xMin, yMin, xMax, yMax, delegate(T p)
            {	//Does it satisfy the predicate?
                if ((_defPredicate != null && !_defPredicate(p)))
                    return false;

                Helpers.ObjectState state = p.getState();
                int px = state.positionX;
                int py = state.positionY;
                return (xMin <= px && px <= xMax && yMin <= py && py <= yMax);
            });
        }

        /// <summary>
        /// Returns objects inside the box defined by the parameters
        /// </summary>		
        public List<T> getObjsInArea(int xMin, int yMin, int xMax, int yMax, Predicate<T> predicate)
        {
            return getObjsByClosure(xMin, yMin, xMax, yMax, delegate(T p)
            {	//Does it satisfy the predicate?
                if ((_defPredicate != null && !_defPredicate(p)) || !predicate(p))
                    return false;

                Helpers.ObjectState state = p.getState();
                int px = state.positionX;
                int py = state.positionY;
                return (xMin <= px && px <= xMax && yMin <= py && py <= yMax);
            });
        }

        private List<T> getObjsByClosure(int xMin, int yMin, int xMax, int yMax, Func<T, bool> filter)
        {
            //Clamp coordinates
            xMin = Math.Max(0, xMin);
            yMin = Math.Max(0, yMin);
            xMax = Math.Min(TICK_MAX, xMax);
            yMax = Math.Min(TICK_MAX, yMax);

            List<T> found = new List<T>();

            // Figure out the buckets we need to search
            int bXMin = xMin / BUCKET_TICKS;
            int bXMax = xMax / BUCKET_TICKS;
            int bYMin = yMin / BUCKET_TICKS;
            int bYMax = yMax / BUCKET_TICKS;

            // Get anyone in the buckets that satisfies the exact coords
            try
            {
                for (int i = bXMin; i <= bXMax; i++)
                {
                    for (int j = bYMin; j <= bYMax; j++)
                    {
                        foreach (T p in _matrix[i, j].ToArray()) //Fix is .ToArray
                        {
                            if (filter(p)) found.Add(p); //Error line
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, "getObjsByClosure " + e);
            }

            return found;
        }

        /// <summary>
        /// Returns the number of objects inside the box defined by the parameters
        /// </summary>		
        public int getObjcountInArea(int xMin, int yMin, int xMax, int yMax)
        {
            return getObjcountByClosure(xMin, yMin, xMax, yMax, delegate(T p)
            {	//Does it satisfy the predicate?
                if ((_defPredicate != null && !_defPredicate(p)))
                    return false;

                Helpers.ObjectState state = p.getState();
                int px = state.positionX;
                int py = state.positionY;
                return (xMin <= px && px <= xMax && yMin <= py && py <= yMax);
            });
        }

        private int getObjcountByClosure(int xMin, int yMin, int xMax, int yMax, Func<T, bool> filter)
        {
            //Clamp coordinates
            xMin = Math.Max(0, xMin);
            yMin = Math.Max(0, yMin);
            xMax = Math.Min(TICK_MAX, xMax);
            yMax = Math.Min(TICK_MAX, yMax);

            // Figure out the buckets we need to search
            int bXMin = xMin / BUCKET_TICKS;
            int bXMax = xMax / BUCKET_TICKS;
            int bYMin = yMin / BUCKET_TICKS;
            int bYMax = yMax / BUCKET_TICKS;

            // Get anyone in the buckets that satisfies the exact coords
            int result = 0;

            for (int i = bXMin; i <= bXMax; i++)
            {
                for (int j = bYMin; j <= bYMax; j++)
                {
                    foreach (T p in _matrix[i, j])
                    {
                        if (filter(p))
                            result++;
                    }
                }
            }

            return result;
        }

        #region Collection functions

        public int Count
        {
            get { return _idToObj.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Add a player to the player tracker
        /// </summary>		
        public void Add(T p)
        {	//Set the given id
            _idToObj[p.getID()] = p;

            Helpers.ObjectState state = p.getState();
            List<T> bucket = _matrix[state.positionX / BUCKET_TICKS, state.positionY / BUCKET_TICKS];

            _objToBucket[p] = bucket;

            // Insert player into the right bucket
            bucket.Add(p);
        }

        public void Clear()
        {
            resetStructures();
        }

        public bool Contains(T p)
        {
            try
            {
                return _idToObj.ContainsValue(p);
            }
            catch (Exception e)
            {
                Log.write(TLog.Exception, e.ToString());
                return false;
            }
        }

        public void CopyTo(T[] array, int index)
        {
            try
            {
                _idToObj.Values.CopyTo(array, index);
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, String.Format("{0}, (Array Length = {1}, Index = {2})", e.ToString(), array.Count(), index));
                string ushorts = "", values = "";
                ushorts = String.Join(",", _idToObj.Keys);
                values = String.Join(",", _idToObj.Values);
                Log.write(TLog.Warning, ushorts);
                Log.write(TLog.Warning, values);
            }
        }

        public bool Remove(T p)
        {
            if (!_objToBucket.ContainsKey(p)) return false;
            else
            {
                // Remove player from ID map
                _idToObj.Remove(p.getID());

                // Take player out of the spatial mapper
                List<T> lastBucket = _objToBucket[p];
                lastBucket.Remove(p);
                _objToBucket.Remove(p);
                return true;
            }
        }
        #endregion

        #region IEnumerable
        /// <summary>
        /// Be careful while using these. Trying to iterate though the player list is subject to change.
        /// Probably not threadsafe when used with the routing code
        /// </summary>		
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _idToObj.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _idToObj.Values.GetEnumerator();
        }
        #endregion
    }
}