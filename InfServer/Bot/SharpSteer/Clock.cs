// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Diagnostics;

using Axiom.Math;

namespace Bnoerj.AI.Steering
{
	public class Clock
	{
		Stopwatch stopwatch;

		// constructor
		public Clock()
		{
			// default is "real time, variable frame rate" and not paused
			FixedFrameRate = 0;
			PausedState = false;
			AnimationMode = false;
			VariableFrameRateMode = true;

			// real "wall clock" time since launch
			totalRealTime = 0;

			// time simulation has run
			totalSimulationTime = 0;

			// time spent paused
			totalPausedTime = 0;

			// sum of (non-realtime driven) advances to simulation time
			totalAdvanceTime = 0;

			// interval since last simulation time 
			elapsedSimulationTime = 0;

			// interval since last clock update time 
			elapsedRealTime = 0;

			// interval since last clock update,
			// exclusive of time spent waiting for frame boundary when targetFPS>0
			elapsedNonWaitRealTime = 0;

			// "manually" advance clock by this amount on next update
			newAdvanceTime = 0;

			// "Calendar time" when this clock was first updated
			stopwatch = new Stopwatch();

			// clock keeps track of "smoothed" running average of recent frame rates.
			// When a fixed frame rate is used, a running average of "CPU load" is
			// kept (aka "non-wait time", the percentage of each frame time (time
			// step) that the CPU is busy).
			smoothedFPS = 0;
			smoothedUsage = 0;
		}

		// update this clock, called exactly once per simulation step ("frame")
		public void Update()
		{
			// keep track of average frame rate and average usage percentage
			UpdateSmoothedRegisters();

			// wait for next frame time (when targetFPS>0)
			// XXX should this be at the end of the update function?
			FrameRateSync();

			// save previous real time to measure elapsed time
			float previousRealTime = totalRealTime;

			// real "wall clock" time since this application was launched
			totalRealTime = RealTimeSinceFirstClockUpdate();

			// time since last clock update
			elapsedRealTime = totalRealTime - previousRealTime;

			// accumulate paused time
			if (paused) totalPausedTime += elapsedRealTime;

			// save previous simulation time to measure elapsed time
			float previousSimulationTime = totalSimulationTime;

			// update total simulation time
			if (AnimationMode)
			{
				// for "animation mode" use fixed frame time, ignore real time
				float frameDuration = 1.0f / FixedFrameRate;
				totalSimulationTime += paused ? newAdvanceTime : frameDuration;
				if (!paused)
				{
					newAdvanceTime += frameDuration - elapsedRealTime;
				}
			}
			else
			{
				// new simulation time is total run time minus time spent paused
				totalSimulationTime = (totalRealTime + totalAdvanceTime - totalPausedTime);
			}


			// update total "manual advance" time
			totalAdvanceTime += newAdvanceTime;

			// how much time has elapsed since the last simulation step?
			if (paused)
			{
				elapsedSimulationTime = newAdvanceTime;
			}
			else
			{
				elapsedSimulationTime = (totalSimulationTime - previousSimulationTime);
			}

			// reset advance amount
			newAdvanceTime = 0;
		}

		// returns the number of seconds of real time (represented as a float)
		// since the clock was first updated.
		public float RealTimeSinceFirstClockUpdate()
		{
			if (stopwatch.IsRunning == false)
			{
				stopwatch.Start();
			}
			return (float)stopwatch.Elapsed.TotalSeconds;
		}

		// force simulation time ahead, ignoring passage of real time.
		// Used for OpenSteerDemo's "single step forward" and animation mode
		float AdvanceSimulationTimeOneFrame()
		{
			// decide on what frame time is (use fixed rate, average for variable rate)
			float fps = (VariableFrameRateMode ? SmoothedFPS : FixedFrameRate);
			float frameTime = 1.0f / fps;

			// bump advance time
			AdvanceSimulationTime(frameTime);

			// return the time value used (for OpenSteerDemo)
			return frameTime;
		}

		void AdvanceSimulationTime(float seconds)
		{
			if (seconds < 0)
				throw new ArgumentException("Negative argument to advanceSimulationTime.", "seconds");
			else
				newAdvanceTime += seconds;
		}

		// "wait" until next frame time
		void FrameRateSync()
		{
			// when in real time fixed frame rate mode
			// (not animation mode and not variable frame rate mode)
			if ((!AnimationMode) && (!VariableFrameRateMode))
			{
				// find next (real time) frame start time
				float targetStepSize = 1.0f / FixedFrameRate;
				float now = RealTimeSinceFirstClockUpdate();
				int lastFrameCount = (int)(now / targetStepSize);
				float nextFrameTime = (lastFrameCount + 1) * targetStepSize;

				// record usage ("busy time", "non-wait time") for OpenSteerDemo app
				elapsedNonWaitRealTime = now - totalRealTime;

				//FIXME: eek.
				// wait until next frame time
				do { } while (RealTimeSinceFirstClockUpdate() < nextFrameTime);
			}
		}


		// main clock modes: variable or fixed frame rate, real-time or animation
		// mode, running or paused.

		// run as fast as possible, simulation time is based on real time
		bool variableFrameRateMode;

		// fixed frame rate (ignored when in variable frame rate mode) in
		// real-time mode this is a "target", in animation mode it is absolute
		int fixedFrameRate;

		// used for offline, non-real-time applications
		bool animationMode;

		// is simulation running or paused?
		bool paused;

		public int FixedFrameRate
		{
			get { return fixedFrameRate; }
			set { fixedFrameRate = value; }
		}

		public bool AnimationMode
		{
			get { return animationMode; }
			set { animationMode = value; }
		}

		public bool VariableFrameRateMode
		{
			get { return variableFrameRateMode; }
			set { variableFrameRateMode = value; }
		}

		public bool TogglePausedState()
		{
			return (paused = !paused);
		}

		public bool PausedState
		{
			get { return paused; }
			set { paused = value; }
		}

		// clock keeps track of "smoothed" running average of recent frame rates.
		// When a fixed frame rate is used, a running average of "CPU load" is
		// kept (aka "non-wait time", the percentage of each frame time (time
		// step) that the CPU is busy).
		float smoothedFPS;
		float smoothedUsage;

		void UpdateSmoothedRegisters()
		{
			float rate = SmoothingRate;
			if (elapsedRealTime > 0)
				Utilities.BlendIntoAccumulator(rate, 1 / elapsedRealTime, ref smoothedFPS);
			if (!VariableFrameRateMode)
				Utilities.BlendIntoAccumulator(rate, Usage, ref smoothedUsage);
		}

		public float SmoothedFPS
		{
			get { return smoothedFPS; }
		}
		public float SmoothedUsage
		{
			get { return smoothedUsage; }
		}
		public float SmoothingRate
		{
			get { return smoothedFPS == 0 ? 1 : elapsedRealTime * 1.5f; }
		}
		public float Usage
		{
			// run time per frame over target frame time (as a percentage)
			get { return ((100 * elapsedNonWaitRealTime) / (1.0f / fixedFrameRate)); }
		}

		// clock state member variables and public accessors for them

		// real "wall clock" time since launch
		float totalRealTime;

		// total time simulation has run
		float totalSimulationTime;

		// total time spent paused
		float totalPausedTime;

		// sum of (non-realtime driven) advances to simulation time
		float totalAdvanceTime;

		// interval since last simulation time
		// (xxx does this need to be stored in the instance? xxx)
		float elapsedSimulationTime;

		// interval since last clock update time 
		// (xxx does this need to be stored in the instance? xxx)
		float elapsedRealTime;

		// interval since last clock update,
		// exclusive of time spent waiting for frame boundary when targetFPS>0
		float elapsedNonWaitRealTime;

		public float TotalRealTime
		{
			get { return totalRealTime; }
		}
		public float TotalSimulationTime
		{
			get { return totalSimulationTime; }
		}
		public float TotalPausedTime
		{
			get { return totalPausedTime; }
		}
		public float TotalAdvanceTime
		{
			get { return totalAdvanceTime; }
		}
		public float ElapsedSimulationTime
		{
			get { return elapsedSimulationTime; }
		}
		public float ElapsedRealTime
		{
			get { return elapsedRealTime; }
		}
		public float ElapsedNonWaitRealTime
		{
			get { return elapsedNonWaitRealTime; }
		}

		// "manually" advance clock by this amount on next update
		float newAdvanceTime;
	}
}
