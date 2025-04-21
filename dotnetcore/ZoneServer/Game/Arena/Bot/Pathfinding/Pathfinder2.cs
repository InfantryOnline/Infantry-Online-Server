using Assets;
using Axiom.Math;
using EpPathFinding.cs;
using InfServer.Bots;
using InfServer.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Assets.LvlInfo;

namespace InfServer.Bots
{
    public class Pathfinder2 : IPathfinder
    {
        private LogClient _logger;
        private LvlInfo lvlInfo;

        private int logcounter = 1;

        JumpPointParam clearanceJpParam;
        BaseGrid clearanceGrid;

        BaseGrid rawGrid;
        JumpPointParam rawJpParam;

        private Thread pathingThread;
        public BlockingCollection<PathfindReq> pathingQueue;

        public Pathfinder2(ZoneServer server, LogClient logger)
        {
            _logger = logger;

            lvlInfo = server._assets.Level;
            pathingQueue = new BlockingCollection<PathfindReq>(25);

            var clearances = new bool[lvlInfo.Width][];

            for (int i = 0; i < lvlInfo.Width; ++i)
            {
                clearances[i] = new bool[lvlInfo.Height];

                for (int j = 0; j < lvlInfo.Height; ++j)
                {
                    clearances[i][j] = !lvlInfo.Tiles[(j * lvlInfo.Width) + i].Blocked;
                }
            }

            rawGrid = new StaticGrid(lvlInfo.Width, lvlInfo.Height, clearances);
            rawJpParam = new JumpPointParam(rawGrid, EndNodeUnWalkableTreatment.DISALLOW, DiagonalMovement.Always);

            // Give a 2-tile "clearance" around blocked tiles for larger vehicles.

            int clearance = 2;

            for (int i = 0; i < lvlInfo.Height; ++i)
            {
                for (int j = 0; j < lvlInfo.Width; ++j)
                {
                    if (lvlInfo.Tiles[(i * lvlInfo.Width) + j].Blocked)
                    {   //Block tiles around it for clearance
                        int yMax = Math.Min(lvlInfo.Height - 1, (i + clearance + 1));
                        int xMax = Math.Min(lvlInfo.Width - 1, (j + clearance + 1));

                        for (int y = Math.Max(0, i - clearance); y < yMax; ++y)
                        {
                            for (int x = Math.Max(0, j - clearance); x < xMax; ++x)
                            {
                                clearances[x][y] = false;
                            }
                        }
                    }
                }
            }

            clearanceGrid = new StaticGrid(lvlInfo.Width, lvlInfo.Height, clearances);
            clearanceJpParam = new JumpPointParam(clearanceGrid, EndNodeUnWalkableTreatment.DISALLOW, DiagonalMovement.Always);
        }

        /// <summary>
        /// Generic Destructor
        /// </summary>
        ~Pathfinder2()
        {
            // FIXME: Time to use cancellation tokens.

            // pathingThread.Abort();
        }

        /// <summary>
        /// Begins the thread used to perform all pathfinding
        /// </summary>
        public void beginThread()
        {
            Log.assume(_logger);

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
            foreach (PathfindReq req in pathingQueue.GetConsumingEnumerable())
            {
                var stopwatch = Stopwatch.StartNew();

                List<GridPos> path;

                if (!calculatePath(req.startX, req.startY, req.endX, req.endY, out path))
                {
                    req.callback(null, 0);
                    continue;
                }

                //Create a steerable path
                req.callback(createSteerablePath(path), path.Count);

                //Required to ensure the delegate is released when the loop is idle
                req.callback = null;

                stopwatch.Stop();

                //Console.WriteLine("Pathing done in: " + stopwatch.ElapsedMilliseconds + " for count of: " + path.Count);
            }
        }

        /// <summary>
        /// Queues a pathfinding operation
        /// </summary>
        public void queueRequest(short startX, short startY, short endX, short endY, Action<List<Vector3>, int> callback)
        {

            //Increased from 25 to 50, there really is no need to limit this, however if we hit over 50 we can clear some.
            if (pathingQueue.Count > 50)
            {
                //Add to logcounter
                logcounter++;

                //write single log for combined count of 20
                if (logcounter == 20)
                {
                    Log.write(TLog.Warning, "Excessive pathing queue count: " + pathingQueue.Count);
                    //Clear log list to start again.
                    logcounter = 1;
                }

                //Remove all path requests currently in pathing queue
                PathfindReq cItem = pathingQueue.Take();
                pathingQueue.TryTake(out cItem, TimeSpan.FromMilliseconds(100));

                //Let them know
                callback(null, 0);
                return;
            }
            else
            {
                PathfindReq req = new PathfindReq();

                req.startX = startX;
                req.startY = startY;
                req.endX = endX;
                req.endY = endY;
                req.callback = callback;

                pathingQueue.Add(req);
            }
        }

        /// <summary>
        /// Calculates a path from start to finish
        /// </summary>
        private bool calculatePath(short startX, short startY, short endX, short endY, out List<GridPos> path)
        {
            var start = new GridPos(startX, startY);
            var end = new GridPos(endX, endY);

            try
            {
                clearanceJpParam.Reset(start, end);

                path = JumpPointFinder.FindPath(clearanceJpParam);

                if (path.Count > 0)
                {
                    return true;
                }
                else
                {
                    rawJpParam.Reset(start, end);

                    path = JumpPointFinder.FindPath(rawJpParam);

                    if (path.Count > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
               Console.WriteLine("Error while pathfinding: " + e.ToString());   
            }

            //var startStr = $"Start: ({startX}, {startY})";
            //var endStr = $"End: ({endX}, {endY})";

            //var walkableStart = rawGrid.IsWalkableAt(start) ? "Start Walkable" : "Start not walkable";
            //var walkableEnd = rawGrid.IsWalkableAt(end) ? "End Walkable" : "End not walkable";

            //Console.WriteLine($"No path found for: {startStr} => {endStr} ({walkableStart}, {walkableEnd})");

            path = null;
            return false;
        }

        /// <summary>
        /// Calculates a path from start to finish
        /// </summary>
        private List<Vector3> createSteerablePath(List<GridPos> path)
        {
            if (path.Count == 0)
            {
                return null;
            }     

            List<Vector3> points = new List<Vector3>(path.Count);

            foreach(var p in path)
            {
                //
                // NOTE: "Why is this 100.0f here?"
                //
                //       The `Position` value in Helpers.ObjectState is also divided by 100.0f which is often where
                //       this value is used. We believe the original implementor scaled the value to make it play
                //       nicer with the steering library.
                //

                float x = (p.x * 16.0f) / 100.0f;
                float y = (p.y * 16.0f) / 100.0f;

                points.Add(new Vector3(x, y, 0));
            }

            return points;
        }

        /// <summary>
        /// Counts paths in queuelist
        /// </summary>
        public int queueCount()
        {
            return pathingQueue.Count;
        }

        public void updateTile(int tileX, int tileY, Tile tile)
        {
            rawGrid.SetWalkableAt(tileX, tileY, !tile.Blocked);
            clearanceGrid.SetWalkableAt(tileX, tileY, !tile.Blocked);
        }
    }
}
