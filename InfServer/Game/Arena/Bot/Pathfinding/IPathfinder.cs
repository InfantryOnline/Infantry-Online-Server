using Assets;
using Axiom.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.LvlInfo;

namespace InfServer.Bots
{
    public class PathfindReq
    {
        public short startX;
        public short startY;
        public short endX;
        public short endY;

        public Action<List<Vector3>, int> callback;
    }

    public interface IPathfinder
    {
        void beginThread();

        void queueRequest(short startX, short startY, short endX, short endY, Action<List<Vector3>, int> callback);

        void updateTile(int tileX, int tileY, Tile tile);

        int queueCount();
    }
}
