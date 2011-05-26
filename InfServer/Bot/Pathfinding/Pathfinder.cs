using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

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
		private int pathHandle;

		#region Pathfinder DLL Declarations
		[DllImport("pathfinder.dll")]
		public static extern int createMapContext([MarshalAs(UnmanagedType.LPArray)] byte[] obstacleMap, int width, int height);

		[DllImport("pathfinder.dll")]
		public static extern bool explorePath(int pathHandle, int start, int target);

		[DllImport("pathfinder.dll")]
		public static extern IntPtr getPath(int pathHandle);

		[DllImport("pathfinder.dll")]
		public static extern int getPathLength(int pathHandle);
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

			//Create a boolean representation of our map
			byte[] map = new byte[lvlInfo.Width * lvlInfo.Height];

			for (int i = 0; i < lvlInfo.Height; ++i)
				for (int j = 0; j < lvlInfo.Width; ++j)
					map[(i * lvlInfo.Width) + j] = lvlInfo.Tiles[(j * lvlInfo.Width) + i].Blocked ? (byte)1 : (byte)0;

			//Initialize our pathfinder
			pathHandle = createMapContext(map, lvlInfo.Width, lvlInfo.Height);
		}

		/// <summary>
		/// Calculates a path from start to finish
		/// </summary>
		public bool calculatePath(short startX, short startY, short endX, short endY, out int[] path)
		{	//Be threadsafe!
			lock (this)
			{	//Convert the coordinates into node numbers
				int start = (startX * lvlInfo.Width) + startY;
				int end = (endX * lvlInfo.Width) + endY;
				path = null;

				//Attempt to solve the path
				try
				{
					bool bSuccess = explorePath(pathHandle, start, end);
					if (!bSuccess)
						return false;
				}
				catch (Exception e)
				{
					Log.write(TLog.Exception, "Error while pathfinding: " + e.ToString());
				}

				//Obtain our path
				int pathSize = getPathLength(pathHandle);
				IntPtr solvedPath = getPath(pathHandle);

				if (pathSize == 0 || solvedPath == IntPtr.Zero)
					return false;

				//Copy it over and we're done
				path = new int[pathSize];
				Marshal.Copy(solvedPath, path, 0, pathSize);
			}

			return true;
		}

		/// <summary>
		/// Calculates a path from start to finish
		/// </summary>
		public List<Vector3> createSteerablePath(int[] path)
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
			lastPointX = path[0] / lvlInfo.Width;
			lastPointY = path[0] % lvlInfo.Width;

			points.Add(new Vector3(((float)(lastPointX) * 16) / 100, ((float)(lastPointY) * 16) / 100, 0));

			for (int i = 1; i < path.Length; ++i)
			{	//Determine whether the next node is in the same direction
				int pointX = path[i] / lvlInfo.Width;
				int pointY = path[i] % lvlInfo.Width;
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
