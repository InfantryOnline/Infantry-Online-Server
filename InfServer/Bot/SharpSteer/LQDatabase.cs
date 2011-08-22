// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Collections.Generic;
using Axiom.Math;

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// This structure represents the spatial database.  Typically one of
	/// these would be created, by a call to lqCreateDatabase, for a given
	/// application.
	/// </summary>
	class LocalityQueryDB
	{
		// type for a pointer to a function used to map over client objects
		public delegate void LQCallBackFunction(Object clientObject, float distanceSquared, Object clientQueryState);

		/// <summary>
		/// This structure is a proxy for (and contains a pointer to) a client
		/// (application) obj in the spatial database.  One of these exists
		/// for each client obj.  This might be included within the
		/// structure of a client obj, or could be allocated separately.
		/// </summary>
		public class ClientProxy
		{
			// previous obj in this bin, or null
			public ClientProxy Prev;

			// next obj in this bin, or null
			public ClientProxy Next;

			// bin ID (pointer to pointer to bin contents list)
			//public ClientProxy bin;
			// bin ID (index into bin contents list)
			public int? Bin;

			// pointer to client obj
			public Object Obj;

			// the obj's location ("key point") used for spatial sorting
			public Vector3 Position;

			public ClientProxy(Object obj)
			{
				Obj = obj;
			}
		}

		// the origin is the super-brick corner minimum coordinates
		Vector3 Origin;

		// length of the edges of the super-brick
		Vector3 Size;

		// number of sub-brick divisions in each direction
		int DivX;
		int DivY;
		int DivZ;

		// pointer to an array of pointers, one for each bin
		// The last index is the extra bin for "everything else" (Points outside super-brick)
		ClientProxy[] bins;

		// extra bin for "everything else" (Points outside super-brick)
		//ClientProxy other;

		/*
		 * Allocate and initialize an LQ database, return a pointer to it.
		 * The application needs to call this before using the LQ facility.
		 * The nine parameters define the properties of the "super-brick":
		 * (1) origin: coordinates of one corner of the super-brick, its
		 *     minimum x, y and z extent.
		 * (2) size: the width, height and depth of the super-brick.
		 * (3) the number of subdivisions (sub-bricks) along each axis.
		 * This routine also allocates the bin array, and initialize its
		 * contents.
		 */
		public LocalityQueryDB(Vector3 origin, Vector3 size, int divx, int divy, int divz)
		{
			Origin = origin;
			Size = size;
			DivX = divx;
			DivY = divy;
			DivZ = divz;

			// The last index is the "other" bin
			int bincount = divx * divy * divz + 1;
			bins = new ClientProxy[bincount];
			for (int i = 0; i < bins.Length; i++)
			{
				bins[i] = null;
			}
		}

		/* Determine index into linear bin array given 3D bin indices */
		public int BinCoordsToBinIndex(int ix, int iy, int iz)
		{
			return ((ix * DivY * DivZ) + (iy * DivZ) + iz);
		}

		/* Call for each client obj every time its location changes.  For
		   example, in an animation application, this would be called each
		   frame for every moving obj.  */
		public void UpdateForNewLocation(ref ClientProxy obj, Vector3 position)
		{
			/* find bin for new location */
			int newBin = BinForLocation(position);

			/* store location in client obj, for future reference */
			obj.Position = position;

			/* has obj moved into a new bin? */
			if (newBin != obj.Bin)
			{
				RemoveFromBin(ref obj);
				AddToBin(ref obj, newBin);
			}
		}

		/* Adds a given client obj to a given bin, linking it into the bin
		   contents list. */
		public void AddToBin(ref ClientProxy obj, int binIndex)
		{
			/* if bin is currently empty */
			if (bins[binIndex] == null)
			{
				obj.Prev = null;
				obj.Next = null;
			}
			else
			{
				obj.Prev = null;
				obj.Next = bins[binIndex];
				bins[binIndex].Prev = obj;
			}

			bins[binIndex] = obj;

			/* record bin ID in proxy obj */
			obj.Bin = binIndex;
		}

		/* Find the bin ID for a location in space.  The location is given in
		   terms of its XYZ coordinates.  The bin ID is a pointer to a pointer
		   to the bin contents list.  */
		public /*lqClientProxy*/int BinForLocation(Vector3 position)
		{
			/* if point outside super-brick, return the "other" bin */
			if (position.x < Origin.x || position.y < Origin.y || position.z < Origin.z ||
				position.x >= Origin.x + Size.x || position.y >= Origin.y + Size.y || position.z >= Origin.z + Size.z)
			{
				return bins.Length - 1;
			}

			/* if point inside super-brick, compute the bin coordinates */
			int ix = (int)(((position.x - Origin.x) / Size.x) * DivX);
			int iy = (int)(((position.y - Origin.y) / Size.y) * DivY);
			int iz = (int)(((position.z - Origin.z) / Size.z) * DivZ);

			/* convert to linear bin number */
			int i = BinCoordsToBinIndex(ix, iy, iz);

			/* return pointer to that bin */
			return i;// (bins[i]);
		}

		/* Apply an application-specific function to all objects in a certain
		   locality.  The locality is specified as a sphere with a given
		   center and radius.  All objects whose location (key-point) is
		   within this sphere are identified and the function is applied to
		   them.  The application-supplied function takes three arguments:

			 (1) a void* pointer to an lqClientProxy's "object".
			 (2) the square of the distance from the center of the search
				 locality sphere (x,y,z) to object's key-point.
			 (3) a void* pointer to the caller-supplied "client query state"
				 object -- typically NULL, but can be used to store state
				 between calls to the lqCallBackFunction.

		   This routine uses the LQ database to quickly reject any objects in
		   bins which do not overlap with the sphere of interest.  Incremental
		   calculation of index values is used to efficiently traverse the
		   bins of interest. */
		public void MapOverAllObjectsInLocality(Vector3 center, float radius, LQCallBackFunction func, Object clientQueryState)
		{
			int partlyOut = 0;
			bool completelyOutside =
				(((center.x + radius) < Origin.x) ||
				 ((center.y + radius) < Origin.y) ||
				 ((center.z + radius) < Origin.z) ||
				 ((center.x - radius) >= Origin.x + Size.x) ||
				 ((center.y - radius) >= Origin.y + Size.y) ||
				 ((center.z - radius) >= Origin.z + Size.z));
			int minBinX, minBinY, minBinZ, maxBinX, maxBinY, maxBinZ;

			/* is the sphere completely outside the "super brick"? */
			if (completelyOutside)
			{
				MapOverAllOutsideObjects(center, radius, func, clientQueryState);
				return;
			}

			/* compute min and max bin coordinates for each dimension */
			minBinX = (int)((((center.x - radius) - Origin.x) / Size.x) * DivX);
			minBinY = (int)((((center.y - radius) - Origin.y) / Size.y) * DivY);
			minBinZ = (int)((((center.z - radius) - Origin.z) / Size.z) * DivZ);
			maxBinX = (int)((((center.x + radius) - Origin.x) / Size.x) * DivX);
			maxBinY = (int)((((center.y + radius) - Origin.y) / Size.y) * DivY);
			maxBinZ = (int)((((center.z + radius) - Origin.z) / Size.z) * DivZ);

			/* clip bin coordinates */
			if (minBinX < 0) { partlyOut = 1; minBinX = 0; }
			if (minBinY < 0) { partlyOut = 1; minBinY = 0; }
			if (minBinZ < 0) { partlyOut = 1; minBinZ = 0; }
			if (maxBinX >= DivX) { partlyOut = 1; maxBinX = DivX - 1; }
			if (maxBinY >= DivY) { partlyOut = 1; maxBinY = DivY - 1; }
			if (maxBinZ >= DivZ) { partlyOut = 1; maxBinZ = DivZ - 1; }

			/* map function over outside objects if necessary (if clipped) */
			if (partlyOut != 0)
				MapOverAllOutsideObjects(center, radius, func, clientQueryState);

			/* map function over objects in bins */
			MapOverAllObjectsInLocalityClipped(
				center, radius,
				func,
				clientQueryState,
				minBinX, minBinY, minBinZ,
				maxBinX, maxBinY, maxBinZ);
		}

		/* Given a bin's list of client proxies, traverse the list and invoke
		the given lqCallBackFunction on each obj that falls within the
		search radius.  */
		public void TraverseBinClientObjectList(ref ClientProxy co, float radiusSquared, LQCallBackFunction func, Object state, Vector3 position)
		{
			while (co != null)
			{
				// compute distance (squared) from this client obj to given
				// locality sphere's centerpoint
				Vector3 d = position - co.Position;
				float distanceSquared = d.LengthSquared;

				// apply function if client obj within sphere
				if (distanceSquared < radiusSquared)
					func(co.Obj, distanceSquared, state);

				// consider next client obj in bin list
				co = co.Next;
			}
		}

		/* This subroutine of lqMapOverAllObjectsInLocality efficiently
		   traverses of subset of bins specified by max and min bin
		   coordinates. */
		public void MapOverAllObjectsInLocalityClipped(Vector3 center, float radius,
							   LQCallBackFunction func,
							   Object clientQueryState,
							   int minBinX, int minBinY, int minBinZ,
							   int maxBinX, int maxBinY, int maxBinZ)
		{
			int i, j, k;
			int iindex, jindex, kindex;
			int slab = DivY * DivZ;
			int row = DivZ;
			int istart = minBinX * slab;
			int jstart = minBinY * row;
			int kstart = minBinZ;
			ClientProxy co;
			ClientProxy bin;
			float radiusSquared = radius * radius;

			/* loop for x bins across diameter of sphere */
			iindex = istart;
			for (i = minBinX; i <= maxBinX; i++)
			{
				/* loop for y bins across diameter of sphere */
				jindex = jstart;
				for (j = minBinY; j <= maxBinY; j++)
				{
					/* loop for z bins across diameter of sphere */
					kindex = kstart;
					for (k = minBinZ; k <= maxBinZ; k++)
					{
						/* get current bin's client obj list */
						bin = bins[iindex + jindex + kindex];
						co = bin;

						/* traverse current bin's client obj list */
						TraverseBinClientObjectList(ref co,
							radiusSquared,
							func,
							clientQueryState,
							center);
						kindex += 1;
					}
					jindex += row;
				}
				iindex += slab;
			}
		}

		/* If the query region (sphere) extends outside of the "super-brick"
		   we need to check for objects in the catch-all "other" bin which
		   holds any object which are not inside the regular sub-bricks  */
		public void MapOverAllOutsideObjects(Vector3 center, float radius, LQCallBackFunction func, Object clientQueryState)
		{
			ClientProxy co = bins[bins.Length - 1];
			float radiusSquared = radius * radius;

			// traverse the "other" bin's client object list
			TraverseBinClientObjectList(ref co, radiusSquared, func, clientQueryState, center);
		}

		/* public helper function */
		public void MapOverAllObjectsInBin(ClientProxy binProxyList, LQCallBackFunction func, Object clientQueryState)
		{
			// walk down proxy list, applying call-back function to each one
			while (binProxyList != null)
			{
				func(binProxyList.Obj, 0, clientQueryState);
				binProxyList = binProxyList.Next;
			}
		}

		/* Apply a user-supplied function to all objects in the database,
		   regardless of locality (cf lqMapOverAllObjectsInLocality) */
		public void MapOverAllObjects(LQCallBackFunction func, Object clientQueryState)
		{
			for (int i = 0; i < bins.Length; i++)
			{
				MapOverAllObjectsInBin(bins[i], func, clientQueryState);
			}
			//MapOverAllObjectsInBin(other, func, clientQueryState);
		}

		/* Removes a given client obj from its current bin, unlinking it
		   from the bin contents list. */
		public void RemoveFromBin(ref ClientProxy obj)
		{
			/* adjust pointers if obj is currently in a bin */
			if (obj.Bin != null)
			{
				/* If this obj is at the head of the list, move the bin
				   pointer to the next item in the list (might be null). */
				if (bins[obj.Bin.Value] == obj)
					bins[obj.Bin.Value] = obj.Next;

				/* If there is a prev obj, link its "next" pointer to the
				   obj after this one. */
				if (obj.Prev != null)
					obj.Prev.Next = obj.Next;

				/* If there is a next obj, link its "prev" pointer to the
				   obj before this one. */
				if (obj.Next != null)
					obj.Next.Prev = obj.Prev;
			}

			/* Null out prev, next and bin pointers of this obj. */
			obj.Prev = null;
			obj.Next = null;
			obj.Bin = null;
		}

		/* Removes (all proxies for) all objects from all bins */
		public void RemoveAllObjects()
		{
			for (int i = 0; i < bins.Length; i++)
			{
				RemoveAllObjectsInBin(ref bins[i]);
			}
			//RemoveAllObjectsInBin(ref other);
		}

		/* public helper function */
		void RemoveAllObjectsInBin(ref ClientProxy bin)
		{
			while (bin != null)
			{
				RemoveFromBin(ref bin);
			}
		}

		/* public helper function */
		struct FindNearestState
		{
			public Object ignoreObject;
			public Object nearestObject;
			public float minDistanceSquared;

		}

		static void FindNearestHelper(Object clientObject, float distanceSquared, Object clientQueryState)
		{
			FindNearestState fns = (FindNearestState)clientQueryState;

			/* do nothing if this is the "ignoreObject" */
			if (fns.ignoreObject != clientObject)
			{
				/* record this object if it is the nearest one so far */
				if (fns.minDistanceSquared > distanceSquared)
				{
					fns.nearestObject = clientObject;
					fns.minDistanceSquared = distanceSquared;
				}
			}
		}

		/* Search the database to find the object whose key-point is nearest
		   to a given location yet within a given radius.  That is, it finds
		   the object (if any) within a given search sphere which is nearest
		   to the sphere's center.  The ignoreObject argument can be used to
		   exclude an object from consideration (or it can be NULL).  This is
		   useful when looking for the nearest neighbor of an object in the
		   database, since otherwise it would be its own nearest neighbor.
		   The function returns a void* pointer to the nearest object, or
		   NULL if none is found.  */
		public Object FindNearestNeighborWithinRadius(Vector3 center, float radius, Object ignoreObject)
		{
			/* initialize search state */
			FindNearestState lqFNS;
			lqFNS.nearestObject = null;
			lqFNS.ignoreObject = ignoreObject;
			lqFNS.minDistanceSquared = float.MaxValue;

			/* map search helper function over all objects within radius */
			MapOverAllObjectsInLocality(center, radius, FindNearestHelper, lqFNS);

			/* return nearest object found, if any */
			return lqFNS.nearestObject;
		}
	}
}
