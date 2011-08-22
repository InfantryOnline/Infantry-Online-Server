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
	/// PolylinePathway: a simple implementation of the Pathway protocol.  The path
	/// is a "polyline" a series of line segments between specified Points.  A
	/// radius defines a volume for the path which is the union of a sphere at each
	/// point and a cylinder along each segment.
	/// </summary>
	public class PolylinePathway : Pathway
	{
		public int pointCount;
        public Vector3[] points;
		public float radius;
		public bool cyclic;

		public PolylinePathway()
		{ }

		// construct a PolylinePathway given the number of Points (vertices),
		// an array of Points, and a path radius.
        public PolylinePathway(int _pointCount, Vector3[] _points, float _radius, bool _cyclic)
		{
			Initialize(_pointCount, _points, _radius, _cyclic);
		}

		// construct a PolylinePathway given the number of Points (vertices),
		// an array of Points, and a path radius.
		public PolylinePathway(List<Vector3> _points, float _radius, bool _cyclic)
		{
			Initialize(_points, _radius, _cyclic);
		}

		// utility for constructors in derived classes
        public void Initialize(int _pointCount, Vector3[] _points, float _radius, bool _cyclic)
		{
			// set data members, allocate arrays
			radius = _radius;
			cyclic = _cyclic;
			pointCount = _pointCount;
			totalPathLength = 0;
			if (cyclic) pointCount++;
			lengths = new float[pointCount];
			points = new Vector3[pointCount];
			normals = new Vector3[pointCount];

			// loop over all Points
			for (int i = 0; i < pointCount; i++)
			{
				// copy in point locations, closing cycle when appropriate
				bool closeCycle = cyclic && (i == pointCount - 1);
				int j = closeCycle ? 0 : i;
				points[i] = _points[j];

				// for the end of each segment
				if (i > 0)
				{
					// compute the segment length
					normals[i] = points[i] - points[i - 1];
					lengths[i] = normals[i].LengthSquared;

					// find the normalized vector parallel to the segment
					normals[i] *= 1 / lengths[i];

					// keep running total of segment lengths
					totalPathLength += lengths[i];
				}
			}
		}

		// utility for constructors in derived classes
		public void Initialize(List<Vector3> _points, float _radius, bool _cyclic)
		{
			// set data members, allocate arrays
			radius = _radius;
			cyclic = _cyclic;
			pointCount = _points.Count;
			totalPathLength = 0;
			if (cyclic) pointCount++;
			lengths = new float[pointCount];
			points = new Vector3[pointCount];
			normals = new Vector3[pointCount];

			// loop over all Points
			for (int i = 0; i < _points.Count; i++)
			{
				// copy in point locations, closing cycle when appropriate
				bool closeCycle = cyclic && (i == pointCount - 1);
				int j = closeCycle ? 0 : i;
				points[i] = _points[j];

				// for the end of each segment
				if (i > 0)
				{
					// compute the segment length
					normals[i] = points[i] - points[i - 1];
					lengths[i] = normals[i].LengthSquared;

					// find the normalized vector parallel to the segment
					normals[i] *= 1 / lengths[i];

					// keep running total of segment lengths
					totalPathLength += lengths[i];
				}
			}
		}

		// Given an arbitrary point ("A"), returns the nearest point ("P") on
		// this path.  Also returns, via output arguments, the path tangent at
		// P and a measure of how far A is outside the Pathway's "tube".  Note
		// that a negative distance indicates A is inside the Pathway.
        public override Vector3 MapPointToPath(Vector3 point, out Vector3 tangent, out float outside)
		{
			float d;
			float minDistance = float.MaxValue;
            Vector3 onPath = Vector3.Zero;
			tangent = Vector3.Zero;

			// loop over all segments, find the one nearest to the given point
			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				segmentNormal = normals[i];
				d = PointToSegmentDistance(point, points[i - 1], points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					onPath = chosen;
					tangent = segmentNormal;
				}
			}

			// measure how far original point is outside the Pathway's "tube"
			outside = Vector3.Distance(onPath, point) - radius;

			// return point on path
			return onPath;
		}

		// given an arbitrary point, convert it to a distance along the path
        public override float MapPointToPathDistance(Vector3 point)
		{
			float d;
			float minDistance = float.MaxValue;
			float segmentLengthTotal = 0;
			float pathDistance = 0;

			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				segmentNormal = normals[i];
				d = PointToSegmentDistance(point, points[i - 1], points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					pathDistance = segmentLengthTotal + segmentProjection;
				}
				segmentLengthTotal += segmentLength;
			}

			// return distance along path of onPath point
			return pathDistance;
		}

		// given a distance along the path, convert it to a point on the path
        public override Vector3 MapPathDistanceToPoint(float pathDistance)
		{
			// clip or wrap given path distance according to cyclic flag
			float remaining = pathDistance;
			if (cyclic)
			{
				remaining = pathDistance % totalPathLength;//FIXME: (float)fmod(pathDistance, totalPathLength);
			}
			else
			{
				if (pathDistance < 0) return points[0];
				if (pathDistance >= totalPathLength) return points[pointCount - 1];
			}

			// step through segments, subtracting off segment lengths until
			// locating the segment that contains the original pathDistance.
			// Interpolate along that segment to find 3d point value to return.
			Vector3 result = Vector3.Zero;
			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				if (segmentLength < remaining)
				{
					remaining -= segmentLength;
				}
				else
				{
					float ratio = remaining / segmentLength;
					result = Utilities.Interpolate(ratio, points[i - 1], points[i]);
					break;
				}
			}
			return result;
		}

		// utility methods

		// compute minimum distance from a point to a line segment
        public float PointToSegmentDistance(Vector3 point, Vector3 ep0, Vector3 ep1)
		{
			// convert the test point to be "local" to ep0
			local = point - ep0;

			// find the projection of "local" onto "segmentNormal"
            segmentProjection = Vector3.Dot(segmentNormal, local);

			// handle boundary cases: when projection is not on segment, the
			// nearest point is one of the endpoints of the segment
			if (segmentProjection < 0)
			{
				chosen = ep0;
				segmentProjection = 0;
				return Vector3.Distance(point, ep0);
			}
			if (segmentProjection > segmentLength)
			{
				chosen = ep1;
				segmentProjection = segmentLength;
				return Vector3.Distance(point, ep1);
			}

			// otherwise nearest point is projection point on segment
			chosen = segmentNormal * segmentProjection;
			chosen += ep0;
			return Vector3.Distance(point, chosen);
		}

		// assessor for total path length;
		public float TotalPathLength
		{
			get { return totalPathLength; }
		}

		// XXX removed the "private" because it interfered with derived
		// XXX classes later this should all be rewritten and cleaned up
		// private:

		// xxx shouldn't these 5 just be local variables?
		// xxx or are they used to pass secret messages between calls?
		// xxx seems like a bad design
		protected float segmentLength;
		protected float segmentProjection;
        protected Vector3 local;
        protected Vector3 chosen;
        protected Vector3 segmentNormal;

		protected float[] lengths;
        protected Vector3[] normals;
		protected float totalPathLength;
	}
}
