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
	public class Utilities
	{
		static Random rng = new Random();

		public static float Interpolate(float alpha, float x0, float x1)
		{
			return x0 + ((x1 - x0) * alpha);
		}

        public static Vector3 Interpolate(float alpha, Vector3 x0, Vector3 x1)
		{
			return x0 + ((x1 - x0) * alpha);
		}

		// ----------------------------------------------------------------------------
		// Random number utilities

		// Returns a float randomly distributed between 0 and 1
		public static float Random()
		{
			return (float)rng.NextDouble();
		}

		// Returns a float randomly distributed between lowerBound and upperBound
		public static float Random(float lowerBound, float upperBound)
		{
			return lowerBound + (Random() * (upperBound - lowerBound));
		}

		/// <summary>
		/// Constrain a given value (x) to be between two (ordered) bounds min
		/// and max.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns>Returns x if it is between the bounds, otherwise returns the nearer bound.</returns>
		public static float Clip(float x, float min, float max)
		{
			if (x < min) return min;
			if (x > max) return max;
			return x;
		}

		// ----------------------------------------------------------------------------
		// remap a value specified relative to a pair of bounding values
		// to the corresponding value relative to another pair of bounds.
		// Inspired by (dyna:remap-interval y y0 y1 z0 z1)
		public static float RemapInterval(float x, float in0, float in1, float out0, float out1)
		{
			// uninterpolate: what is x relative to the interval in0:in1?
			float relative = (x - in0) / (in1 - in0);

			// now interpolate between output interval based on relative x
			return Interpolate(relative, out0, out1);
		}

		// Like remapInterval but the result is clipped to remain between
		// out0 and out1
		public static float RemapIntervalClip(float x, float in0, float in1, float out0, float out1)
		{
			// uninterpolate: what is x relative to the interval in0:in1?
			float relative = (x - in0) / (in1 - in0);

			// now interpolate between output interval based on relative x
			return Interpolate(Clip(relative, 0, 1), out0, out1);
		}

		// ----------------------------------------------------------------------------
		// classify a value relative to the interval between two bounds:
		//     returns -1 when below the lower bound
		//     returns  0 when between the bounds (inside the interval)
		//     returns +1 when above the upper bound
		public static int IntervalComparison(float x, float lowerBound, float upperBound)
		{
			if (x < lowerBound) return -1;
			if (x > upperBound) return +1;
			return 0;
		}

		public static float ScalarRandomWalk(float initial, float walkspeed, float min, float max)
		{
			float next = initial + (((Random() * 2) - 1) * walkspeed);
			if (next < min) return min;
			if (next > max) return max;
			return next;
		}

		public static float Square(float x)
		{
			return x * x;
		}

		/// <summary>
		/// blends new values into an accumulator to produce a smoothed time series
		/// </summary>
		/// <remarks>
		/// Modifies its third argument, a reference to the float accumulator holding
		/// the "smoothed time series."
		/// 
		/// The first argument (smoothRate) is typically made proportional to "dt" the
		/// simulation time step.  If smoothRate is 0 the accumulator will not change,
		/// if smoothRate is 1 the accumulator will be set to the new value with no
		/// smoothing.  Useful values are "near zero".
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="smoothRate"></param>
		/// <param name="newValue"></param>
		/// <param name="smoothedAccumulator"></param>
		/// <example>blendIntoAccumulator (dt * 0.4f, currentFPS, smoothedFPS)</example>
		public static void BlendIntoAccumulator(float smoothRate, float newValue, ref float smoothedAccumulator)
		{
			smoothedAccumulator = Interpolate(Clip(smoothRate, 0, 1), smoothedAccumulator, newValue);
		}

		public static void BlendIntoAccumulator(float smoothRate, Vector3 newValue, ref Vector3 smoothedAccumulator)
		{
			smoothedAccumulator = Interpolate(Clip(smoothRate, 0, 1), smoothedAccumulator, newValue);
		}
	}
}
