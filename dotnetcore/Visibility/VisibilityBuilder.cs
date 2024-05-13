using Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visibility
{
    internal enum TileShapeKind
    {
        Clear,
        Solid,
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight
    }

    internal enum TilePhysicsKind
    {
        Clear,

        RedSolid,
        RedUpperLeft,
        RedUpperRight,
        RedLowerLeft,
        RedLowerRight,

        GreenSolid,
        GreenUpperLeft,
        GreenUpperRight,
        GreenLowerLeft,
        GreenLowerRight,

        YellowSolid,
        YellowUpperLeft,
        YellowUpperRight,
        YellowLowerLeft,
        YellowLowerRight,

        OrangeSolid,
        OrangeUpperLeft,
        OrangeUpperRight,
        OrangeLowerLeft,
        OrangeLowerRight,

        PurpleSolid,
        PurpleUpperLeft,
        PurpleUpperRight,
        PurpleLowerLeft,
        PurpleLowerRight,

        RedMoveRight,
        RedMoveLeft,
        RedMoveDown,
        RedMoveUp,

        TealSolid,
        BlueSolid
    }

    /// <summary>
    /// Responsible for constructing a series of line segments that represent the tiles of the map that are blocked.
    /// 
    /// This helps to reduce the number of queries needed to determine if something is visible or not from a given point, and
    /// is also a simpler way of dealing with the geometry, as line segments are in general easier to deal with.
    /// </summary>
    public class VisibilityBuilder(LvlInfo lvl)
    {
        public List<Group> groups = new List<Group>();

        public void Build()
        {
            if (lvl == null)
            {
                throw new ArgumentNullException(nameof(lvl));
            }

            List<LineSegment> segments = new List<LineSegment>();

            for (var x = 0; x < lvl.Width; x++)
            {
                for (var y = 0; y < lvl.Height; y++)
                {
                    var t = lvl.Tiles[indexAt(x, y)];

                    if (!t.Blocked)
                    {
                        continue;
                    }

                    var g = new Group(t.Physics, t.Vision);

                    var groupIdx = groups.IndexOf(g);

                    if (groupIdx == -1)
                    {
                        groups.Add(g);
                        groupIdx = groups.Count - 1;
                    }

                    //
                    // Now that we have the group prepared, we need to assess the tile in question
                    // and it's bordering tiles to determine what kind of segemnts to generate.
                    //
                    // At most, we can generate four line segments, and that is a square tile that
                    // has no surrounding (blocking) tiles.
                    //
                    // Otherwise as we move through the tiles, we output either new segments
                    // or we combine with segments we already have.
                    //

                    var shape = PhysicsToShapeKind((TilePhysicsKind)t.Physics);

                    TileShapeKind top = TileShapeKind.Clear;
                    TileShapeKind bottom = TileShapeKind.Clear;
                    TileShapeKind left = TileShapeKind.Clear;
                    TileShapeKind right = TileShapeKind.Clear;

                    if (y > 0)
                    {
                        top = shapeAt(x, y - 1);
                    }

                    if (x > 0)
                    {
                        left = shapeAt(x - 1, y);
                    }

                    if (y < lvl.Height - 1)
                    {
                        bottom = shapeAt(x, y + 1);
                    }

                    if (x < lvl.Width - 1)
                    {
                        right = shapeAt(x + 1, y);
                    }

                    switch(shape)
                    {
                        case TileShapeKind.Solid:
                            segments.AddRange(GenerateSolid(groupIdx, x, y, top, right, bottom, left));
                            break;

                        case TileShapeKind.LowerLeft:
                            segments.AddRange(GenerateLowerLeft(groupIdx, x, y, top, right, bottom, left));
                            break;

                        case TileShapeKind.LowerRight:
                            segments.AddRange(GenerateLowerRight(groupIdx, x, y, top, right, bottom, left));
                            break;

                        case TileShapeKind.UpperLeft:
                            segments.AddRange(GenerateUpperLeft(groupIdx, x, y, top, right, bottom, left));
                            break;

                        case TileShapeKind.UpperRight:
                            segments.AddRange(GenerateUpperRight(groupIdx, x, y, top, right, bottom, left));
                            break;
                    }
                }
            }
        }

        internal int indexAt(int x, int y) => (y * lvl.Width) + x;

        internal TileShapeKind shapeAt(int x, int y) => PhysicsToShapeKind((TilePhysicsKind)lvl.Tiles[indexAt(x, y)].Physics);

        internal List<LineSegment> GenerateSolid(int g, int x, int y, TileShapeKind top, TileShapeKind right, TileShapeKind bottom, TileShapeKind left)
        {
            var output = new List<LineSegment>();
            
            if (top == TileShapeKind.Clear || top == TileShapeKind.UpperLeft || top == TileShapeKind.UpperRight)
            {
                var a = new Point(x,     y);
                var b = new Point(x + 1, y);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (right == TileShapeKind.Clear || right == TileShapeKind.UpperRight || right == TileShapeKind.LowerRight)
            {
                var a = new Point(x + 1, y);
                var b = new Point(x + 1, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (bottom == TileShapeKind.Clear || bottom == TileShapeKind.LowerLeft || bottom == TileShapeKind.LowerRight)
            {
                var a = new Point(x,     y + 1);
                var b = new Point(x + 1, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (left == TileShapeKind.Clear || left == TileShapeKind.UpperLeft || left == TileShapeKind.LowerLeft)
            {
                var a = new Point(x, y);
                var b = new Point(x, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            return output;
        }

        internal List<LineSegment> GenerateUpperLeft(int g, int x, int y, TileShapeKind top, TileShapeKind right, TileShapeKind bottom, TileShapeKind left)
        {
            var output = new List<LineSegment>();

            if (top == TileShapeKind.Clear || top == TileShapeKind.UpperLeft || top == TileShapeKind.UpperRight)
            {
                var a = new Point(x, y);
                var b = new Point(x + 1, y);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (left == TileShapeKind.Clear || left == TileShapeKind.UpperLeft || left == TileShapeKind.LowerLeft)
            {
                var a = new Point(x, y);
                var b = new Point(x, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            var dA = new Point(x + 1, y);
            var dB = new Point(x, y + 1);
            var dL = new LineSegment { GroupIndex = g, A = dA, B = dB };

            output.Add(dL);

            return output;
        }

        internal List<LineSegment> GenerateUpperRight(int g, int x, int y, TileShapeKind top, TileShapeKind right, TileShapeKind bottom, TileShapeKind left)
        {
            var output = new List<LineSegment>();

            if (top == TileShapeKind.Clear || top == TileShapeKind.UpperLeft || top == TileShapeKind.UpperRight)
            {
                var a = new Point(x, y);
                var b = new Point(x + 1, y);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (right == TileShapeKind.Clear || right == TileShapeKind.UpperRight || right == TileShapeKind.LowerRight)
            {
                var a = new Point(x + 1, y);
                var b = new Point(x + 1, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            var dA = new Point(x, y);
            var dB = new Point(x + 1, y + 1);
            var dL = new LineSegment { GroupIndex = g, A = dA, B = dB };

            output.Add(dL);

            return output;
        }

        internal List<LineSegment> GenerateLowerLeft(int g, int x, int y, TileShapeKind top, TileShapeKind right, TileShapeKind bottom, TileShapeKind left)
        {
            var output = new List<LineSegment>();

            if (bottom == TileShapeKind.Clear || bottom == TileShapeKind.LowerLeft || bottom == TileShapeKind.LowerRight)
            {
                var a = new Point(x, y + 1);
                var b = new Point(x + 1, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (left == TileShapeKind.Clear || left == TileShapeKind.UpperLeft || left == TileShapeKind.LowerLeft)
            {
                var a = new Point(x, y);
                var b = new Point(x, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            var dA = new Point(x, y);
            var dB = new Point(x  + 1, y + 1);
            var dL = new LineSegment { GroupIndex = g, A = dA, B = dB };

            output.Add(dL);

            return output;
        }

        internal List<LineSegment> GenerateLowerRight(int g, int x, int y, TileShapeKind top, TileShapeKind right, TileShapeKind bottom, TileShapeKind left)
        {
            var output = new List<LineSegment>();

            if (bottom == TileShapeKind.Clear || bottom == TileShapeKind.LowerLeft || bottom == TileShapeKind.LowerRight)
            {
                var a = new Point(x, y + 1);
                var b = new Point(x + 1, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            if (right == TileShapeKind.Clear || right == TileShapeKind.UpperRight || right == TileShapeKind.LowerRight)
            {
                var a = new Point(x + 1, y);
                var b = new Point(x + 1, y + 1);
                var l = new LineSegment { GroupIndex = g, A = a, B = b };

                output.Add(l);
            }

            var dA = new Point(x + 1, y);
            var dB = new Point(x, y + 1);
            var dL = new LineSegment { GroupIndex = g, A = dA, B = dB };

            output.Add(dL);

            return output;
        }

        internal TileShapeKind PhysicsToShapeKind(TilePhysicsKind t)
        {
            switch(t)
            {
                case TilePhysicsKind.Clear:
                    return TileShapeKind.Clear;

                // Case 1: Solid []
                case TilePhysicsKind.RedSolid:
                case TilePhysicsKind.BlueSolid:
                case TilePhysicsKind.YellowSolid:
                case TilePhysicsKind.OrangeSolid:
                case TilePhysicsKind.TealSolid:
                case TilePhysicsKind.GreenSolid:
                case TilePhysicsKind.PurpleSolid:
                case TilePhysicsKind.RedMoveDown:
                case TilePhysicsKind.RedMoveLeft:
                case TilePhysicsKind.RedMoveRight:
                case TilePhysicsKind.RedMoveUp:
                    return TileShapeKind.Solid;

                // Case 2: Corner Upper Left     |/
                case TilePhysicsKind.RedUpperLeft:
                case TilePhysicsKind.GreenUpperLeft:
                case TilePhysicsKind.OrangeUpperLeft:
                case TilePhysicsKind.PurpleUpperLeft:
                case TilePhysicsKind.YellowUpperLeft:
                    return TileShapeKind.UpperLeft;

                // Case 3: Corner Upper Right    \|
                case TilePhysicsKind.RedUpperRight:
                case TilePhysicsKind.GreenUpperRight:
                case TilePhysicsKind.OrangeUpperRight:
                case TilePhysicsKind.PurpleUpperRight:
                case TilePhysicsKind.YellowUpperRight:
                    return TileShapeKind.UpperRight;

                // Case 4: Corner Lower Left  |\
                case TilePhysicsKind.RedLowerLeft:
                case TilePhysicsKind.GreenLowerLeft:
                case TilePhysicsKind.OrangeLowerLeft:
                case TilePhysicsKind.PurpleLowerLeft:
                case TilePhysicsKind.YellowLowerLeft:
                    return TileShapeKind.LowerLeft;

                // Case 4: Corner Lower Right  /|
                case TilePhysicsKind.RedLowerRight:
                case TilePhysicsKind.GreenLowerRight:
                case TilePhysicsKind.OrangeLowerRight:
                case TilePhysicsKind.PurpleLowerRight:
                case TilePhysicsKind.YellowLowerRight:
                    return TileShapeKind.LowerRight;
            }

            throw new Exception("Unhandled");
        }
    }
}
