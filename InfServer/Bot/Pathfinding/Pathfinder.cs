using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Game;

using Bnoerj.AI.Steering;
using Axiom.Math;
using Assets;

namespace InfServer.Bots
{
	// Pathfinder Class
	/// Performs pathfinding operations for an arena
	///////////////////////////////////////////////////////
	public class Pathfinder
	{	// Member variables
		///////////////////////////////////////////////////
		private LvlInfo lvlInfo;
		private int pathHandleClr2;
		private int pathHandleClr0;

		private Thread pathingThread;
		private BlockingCollection<PathfindReq> pathingQueue;

		private class PathfindReq
		{
			public short startX;
			public short startY;
			public short endX;
			public short endY;

			public Action<List<Vector3>, int> callback;
		}

		#region Pathfinder DLL Declarations
		[DllImport("pathfinder.dll")]
		public static extern int createMapContext([MarshalAs(UnmanagedType.LPArray)] byte[] obstacleMap, int width, int height);

		[DllImport("pathfinder.dll")]
		public static extern int createSearchContext(int pathHandle, int start, int target);

		[DllImport("pathfinder.dll")]
		public static extern IntPtr getPath(int searchHandle);

		[DllImport("pathfinder.dll")]
		public static extern int getPathLength(int searchHandle);

		[DllImport("pathfinder.dll")]
		public static extern void deleteSearchContext(int searchHandle);

		[DllImport("pathfinder.dll")]
		public static extern void deleteMapContext(int pathHandle);

		[DllImport("pathfinder.dll")]
		public static extern bool isBlocked(int pathHandle, int nodeID);
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic Constructor
		/// </summary>
		public Pathfinder(ZoneServer server)
		{
			lvlInfo = server._assets.Level;
			pathingQueue = new BlockingCollection<PathfindReq>();

			//Create a boolean representation of our map
			byte[] map = new byte[lvlInfo.Width * lvlInfo.Height];

			for (int i = 0; i < lvlInfo.Height; ++i)
				for (int j = 0; j < lvlInfo.Width; ++j)
				{	//Write in the blocked tile
					map[(i * lvlInfo.Width) + j] = lvlInfo.Tiles[(i * lvlInfo.Width) + j].Blocked ? (byte)1 : (byte)0;
				}

			pathHandleClr0 = createMapContext(map, lvlInfo.Width, lvlInfo.Height);

			//Block tiles for clearance
			int clearance = 2;

			for (int i = 0; i < lvlInfo.Height; ++i)
				for (int j = 0; j < lvlInfo.Width; ++j)
				{	
					if (lvlInfo.Tiles[(i * lvlInfo.Width) + j].Blocked)
					{	//Block tiles around it for clearance
						int yMax = Math.Min(lvlInfo.Height - 1, (i + clearance + 1));
						int xMax = Math.Min(lvlInfo.Width - 1, (j + clearance + 1));

						for (int y = Math.Max(0, i - clearance); y < yMax; ++y)
							for (int x = Math.Max(0, j - clearance); x < xMax; ++x)
								map[(y * lvlInfo.Width) + x] = (byte)1;
					}
				}

			//Initialize our pathfinder
			pathHandleClr2 = createMapContext(map, lvlInfo.Width, lvlInfo.Height);
		}

		/// <summary>
		/// Generic Destructor
		/// </summary>
		~Pathfinder()
		{	//Delete our context
			deleteMapContext(pathHandleClr0);
			deleteMapContext(pathHandleClr2);
			pathingThread.Abort();
		}

		/// <summary>
		/// Begins the thread used to perform all pathfinding
		/// </summary>
		public void beginThread()
		{
			pathingThread = new Thread(pathfinder);
			pathingThread.IsBackground = true;
			pathingThread.Name = "Pathfinding";
			pathingThread.Start(pathingThread);
		}

		/// <summary>
		/// Begins the thread used to perform all pathfinding
		/// </summary>
		private void pathfinder(Object obj)
		{
			while (true)
			{	//Do we have any requests to process?
				if (pathingQueue.Count == 0)
				{
					Thread.Sleep(10);
					continue;
				}

				//Take one!
				PathfindReq req = pathingQueue.Take();

				//Solve the path
				int[] path;

				if (!calculatePath(req.startX, req.startY, req.endX, req.endY, out path))
				{
					req.callback(null, 0);
					continue;
				}
				
				//Create a steerable path
				req.callback(createSteerablePath(path), path.Length);
			}
		}

		/// <summary>
		/// Queues a pathfinding operation
		/// </summary>
		public void queueRequest(short startX, short startY, short endX, short endY, Action<List<Vector3>, int> callback)
		{	
			PathfindReq req = new PathfindReq();

			req.startX = startX;
			req.startY = startY;
			req.endX = endX;
			req.endY = endY;
			req.callback = callback;

			pathingQueue.Add(req);
		}

		/// <summary>
		/// Calculates a path from start to finish
		/// </summary>
		private bool calculatePath(short startX, short startY, short endX, short endY, out int[] path)
		{	//Convert the coordinates into node numbers
			int start = (startY * lvlInfo.Width) + startX;
			int end = (endY * lvlInfo.Width) + endX;
			path = null;

			//Attempt to solve the path
			int searchContext = 0;

			try
			{	//First attempt to make a clearance 2 path
				if (!isBlocked(pathHandleClr2, start) && !isBlocked(pathHandleClr2, end))
					searchContext = createSearchContext(pathHandleClr2, start, end);

				//If it fails, attempt a clearance 0 path
				bool bInvalid = (searchContext == 0 || getPath(searchContext) == IntPtr.Zero || getPathLength(searchContext) == 0);

				if (bInvalid)
				{
					if (searchContext != 0)
					{
						deleteSearchContext(searchContext);
						searchContext = 0;
					}

					if (isBlocked(pathHandleClr2, start) || isBlocked(pathHandleClr2, end))
						searchContext = createSearchContext(pathHandleClr0, start, end);
				}
			}
			catch (Exception e)
			{
				Log.write(TLog.Exception, "Error while pathfinding: " + e.ToString());
			}

			if (searchContext == 0)
				return false;

			//Obtain our path
			int pathSize = getPathLength(searchContext);
			IntPtr solvedPath = getPath(searchContext);
			bool bValid = (pathSize != 0 && solvedPath != IntPtr.Zero);

			if (bValid)
			{	//Copy it over and we're done
				path = new int[pathSize];
				Marshal.Copy(solvedPath, path, 0, pathSize);
			}

			deleteSearchContext(searchContext);
			return bValid;
		}

		/// <summary>
		/// Calculates a path from start to finish
		/// </summary>
		private List<Vector3> createSteerablePath(int[] path)
		{	//Sanity
			if (path.Length == 0)
				return null;
			
			//Convert our path integers into a vector path
			List<Vector3> points = new List<Vector3>(path.Length);
			int lastPointX = 0;
			int lastPointY = 0;
			int lastDiffX = 0;
			int lastDiffY = 0;

			//Commit the first node to the path
			lastPointX = path[0] % lvlInfo.Width;
			lastPointY = path[0] / lvlInfo.Width;

			points.Add(new Vector3(((float)(lastPointX) * 16) / 100, ((float)(lastPointY) * 16) / 100, 0));

			for (int i = 1; i < path.Length; ++i)
			{	//Determine whether the next node is in the same direction
				int pointX = path[i] % lvlInfo.Width;
				int pointY = path[i] / lvlInfo.Width;
				int diffX = pointX - lastPointX;
				int diffY = pointY - lastPointY;

				//If there's a change of direction
				if (i != 1 && (lastDiffX != diffX || lastDiffY != diffY))
					//Commit the last node to the path
					points.Add(new Vector3(((float)(lastPointX) * 16) / 100, ((float)(lastPointY) * 16) / 100, 0));

				//Update the 'last' variables
				lastPointX = pointX;
				lastPointY = pointY;
				lastDiffX = diffX;
				lastDiffY = diffY;
			}

			//Add the final point to make sure we get there
			points.Add(new Vector3(((float)(lastPointX) * 16) / 100, ((float)(lastPointY) * 16) / 100, 0));

			//Create our new pathway
			return points;
		}
	}
}
