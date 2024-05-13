namespace Visibility
{
    /// <summary>
    /// 2D point in integer space.
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    public readonly record struct Point(int X, int Y) { }

    /// <summary>
    /// Line Segment group identifier.
    /// </summary>
    /// <param name="physics"></param>
    /// <param name="vision"></param>
    public readonly record struct Group(int physics, int vision) { }

    /// <summary>
    /// Line segment in integer space. Line Segments are grouped according to additional properties.
    /// </summary>
    public class LineSegment
    {
        public int GroupIndex { get; set; }
        public Point A { get; set; } = new Point(0, 0);
        public Point B { get; set; } = new Point(0, 0);

        /// <summary>
        /// Attempts to merge two line segments that have a point of overlap. Returns the merged line segment if successful.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static LineSegment? TryMerge(LineSegment s1, LineSegment s2)
        {
            if (s1.GroupIndex != s2.GroupIndex)
            {
                return null;
            }

            if (s1.A == s2.A)
            {
                return new LineSegment { A = s1.B, B = s2.B };
            }

            if (s1.B == s2.B)
            {
                return new LineSegment { A = s1.A, B = s2.A };
            }

            if (s1.A == s2.B)
            {
                return new LineSegment { A = s1.B, B = s2.A };
            }

            if (s1.B == s2.A)
            {
                return new LineSegment { A = s1.A, B = s2.B };
            }

            return null;
        }
    }
}
