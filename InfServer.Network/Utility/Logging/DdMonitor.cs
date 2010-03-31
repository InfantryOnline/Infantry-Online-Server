// Stephen Toub

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace InfServer
{
	public class DdMonitor
	{
		/// <summary>Lock used to protected private static state in DdMonitor.</summary>
		private static object _globalLock = new object();
		/// <summary>Maps actual monitor being waited on to internal data about that monitor.</summary>
		private static Dictionary<object, MonitorState> _monitorStates = new Dictionary<object, MonitorState>();
		private static Dictionary<object, Thread> _monitorOwners = new Dictionary<object, Thread>();

		private static volatile bool m_bEnabled;
		private static volatile bool m_bNoSync;
		private static bool m_bNoSyncCheck;

		private static int m_defaulttimeout;

		public static int DefaultTimeout
		{
			get
			{
				return m_defaulttimeout;
			}

			set
			{
				m_defaulttimeout = value;
			}
		}

		public static bool bEnabled
		{
			get
			{
				return m_bEnabled;
			}

			set
			{
				m_bEnabled = value;
			}
		}

		public static bool bNoSync
		{
			get
			{
				return m_bNoSync;
			}

			set
			{
				m_bNoSync = value;
			}
		}

		public static bool bNoSyncCheck
		{
			get
			{
				return m_bNoSyncCheck;
			}

			set
			{
				m_bNoSyncCheck = value;
			}
		}

		/// <summary>Locks the specified object and returns an IDisposable that can be used to release the lock.</summary>
		/// <param name="monitor">The object on which to acquire the monitor lock.</param>
		/// <returns>An IDisposable that can be used to release the lock.</returns>
		public static IDisposable Lock(object monitor)
		{
			if (monitor == null) throw new ArgumentNullException("monitor");
			IDisposable cookie = new DdMonitorCookie(monitor);
			if (!Enter(monitor))
				return null;
			return cookie;
		}

		/// <summary>
		/// Enables syntax close to that of lock(obj) {...}.  Rather,
		/// the Lock method will return an instance of this IDisposable
		/// and in its Dispose method it will release the lock, thus enabling
		/// the syntax: using(DdMonitorFirstAttempt.Lock(obj)) {...}
		/// </summary>
		private class DdMonitorCookie : IDisposable
		{
			/// <summary>The lock to be released.</summary>
			private object _monitor;

			/// <summary>Initializes the DdMonitorCookie.</summary>
			/// <param name="obj">The object to be released.</param>
			public DdMonitorCookie(object obj) { _monitor = obj; }

			/// <summary>Exit the lock.</summary>
			public void Dispose()
			{
				if (_monitor != null)
				{
					DdMonitor.Exit(_monitor);
					_monitor = null;
				}
			}
		}

		/// <summary>Acquires an exclusive lock on the specified object.</summary>
		/// <param name="monitor">The object on which to acquire the monitor lock.</param>
		public static bool Enter(object monitor)
		{
			if (!TryEnter(monitor, m_defaulttimeout))
			{	//Make a note
				Log.write(TLog.Error, "Monitor Enter timed out on object '" + monitor.GetType().ToString() + "' ignoring.");

				//Jump on the global lock-train
				lock (_globalLock)
				{	//Obtain the monitor state info
					MonitorState ms = null;

					if (_monitorStates.TryGetValue(monitor, out ms))
					{	//Dump info about the owner thread
						if (ms.traces.ContainsKey(ms.OwningThread))
							Log.write(TLog.Inane, "Holding thread info:\r\n" + ms.traces[ms.OwningThread]);
					}
				}

				return false;
			}

			return true;
		}

		/// <summary>Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object.</summary>
		/// <param name="monitor">The object on which to acquire the lock.</param>
		/// <returns>true if the current thread acquires the lock without blocking; otherwise, false.</returns>
		public static bool TryEnter(object monitor)
		{
			return TryEnter(monitor, 0);
		}

		/// <summary>Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object.</summary>
		/// <param name="monitor">The object on which to acquire the lock.</param>
		/// <param name="timeout">A TimeSpan representing the amount of time to wait for the lock. A value of –1 millisecond specifies an infinite wait.</param>
		/// <returns>true if the current thread acquires the lock without blocking; otherwise, false.</returns>
		public static bool TryEnter(object monitor, TimeSpan timeout)
		{
			long totalMilliseconds = (long)timeout.TotalMilliseconds;
			if (totalMilliseconds < -1 ||
				totalMilliseconds > Int32.MaxValue) throw new ArgumentOutOfRangeException("timeout");
			return TryEnter(monitor, (int)totalMilliseconds);
		}

		/// <summary>Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object.</summary>
		/// <param name="monitor">The object on which to acquire the lock.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait for the lock.</param>
		/// <returns>true if the current thread acquires the lock without blocking; otherwise, false.</returns>
		public static bool TryEnter(object monitor, int millisecondsTimeout)
		{	// Validate arguments
			if (monitor == null) throw new ArgumentNullException("monitor");
			if (millisecondsTimeout < 0 && millisecondsTimeout != Timeout.Infinite) throw new ArgumentOutOfRangeException("millisecondsTimeout");

			//If we're not syncing, don't do anything
			if (m_bNoSync)
				return true;

			//If we're not properly enabled, just do a simple entry
			if (!m_bEnabled)
			{	//If we managed to lock on the object, we need to
				//specify ourself as the owning thread.
				bool bLocked = Monitor.TryEnter(monitor, millisecondsTimeout);
				if (!m_bNoSyncCheck && bLocked)
					lock (_monitorOwners)
						_monitorOwners[monitor] = Thread.CurrentThread;
				return bLocked;
			}

			// Keep track of whether we actually acquired the monitor or not
			bool thisThreadOwnsMonitor = false;
			MonitorState ms = null;
			try
			{
				// Register the current thread as waiting on the monitor.
				// Take the global lock before manipulating shared state.  Note that by our lock order, 
				// we can take _globalLock while holding an individual monitor, but we *can't* take a 
				// monitor while holding _globalLock; otherwise, we'd risk deadlock.
				lock (_globalLock)
				{
					// Get the internal data for this monitor.  If not data exists, create it.
					if (!_monitorStates.TryGetValue(monitor, out ms))
					{
						_monitorStates[monitor] = ms = new MonitorState(monitor);
					}

					// If we already hold this lock, then there's no chance of deadlock by waiting on it,
					// since monitors are reentrant.  If we don't hold the lock, register our intent to 
					// wait on it and check for deadlock.
					if (ms.OwningThread != Thread.CurrentThread)
					{
						ms.WaitingThreads.Add(Thread.CurrentThread);

						//Put a little stack trace in too
						System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
						System.Diagnostics.StackFrame[] frames = st.GetFrames();
						string mytrace = string.Format("{0:X}:{1}\r\n", ms.MonitorObject.GetHashCode(), Thread.CurrentThread.ManagedThreadId);

						foreach (System.Diagnostics.StackFrame sf in frames)
						{	//Write the frame details
							mytrace += "\tat " + sf.GetMethod() + " in " + sf.GetFileName() + " : " + sf.GetFileLineNumber();
							mytrace += "\r\n";
						}

						ms.traces[Thread.CurrentThread] = mytrace;

						ThrowIfDeadlockDetected(ms);
					}
				}

				// Try to enter the monitor
				thisThreadOwnsMonitor = Monitor.TryEnter(monitor, millisecondsTimeout);

				// At this point we now may own the monitor...
				if (!m_bNoSyncCheck && thisThreadOwnsMonitor)
					lock (_globalLock)
						_monitorOwners[monitor] = Thread.CurrentThread;
			}
			finally
			{
				lock (_globalLock)
				{
					if (ms != null) // This would only be null if an exception occurred at a really weird place
					{
						// We're no longer waiting on the monitor, either because something went wrong
						// in the wait or because we now own the monitor
						ms.WaitingThreads.Remove(Thread.CurrentThread);

						// If we did get the monitor, then note that we now own it
						if (thisThreadOwnsMonitor)
						{
							if (ms.OwningThread != Thread.CurrentThread)
							{
								ms.OwningThread = Thread.CurrentThread;
							}
							else ms.ReentranceCount++;
						}
					}
				}
			}

			// Return whether we obtained the monitor or not
			return thisThreadOwnsMonitor;
		}

		/// <summary>Releases an exclusive lock on the specified object.</summary>
		/// <param name="monitor">Releases an exclusive lock on the specified object.</param>
		public static void Exit(object monitor)
		{
			// Validate arguments
			if (monitor == null) throw new ArgumentNullException("monitor");

			//If we're not syncing, don't do anything
			if (m_bNoSync)
				return;

			//If we're not enabled, do a simple exit
			if (!m_bEnabled)
			{
				if (!m_bNoSyncCheck)
					lock (_monitorOwners)
						if (_monitorOwners.ContainsKey(monitor))
							_monitorOwners.Remove(monitor);
				Monitor.Exit(monitor);
				return;
			}

			// Take the global lock to manipulate shared state
			lock (_globalLock)
			{
				if (!m_bNoSyncCheck && _monitorOwners.ContainsKey(monitor))
					_monitorOwners.Remove(monitor);

				// Grab the MonitorState for this monitor.  
				MonitorState ms;
				if (!_monitorStates.TryGetValue(monitor, out ms)) throw new SynchronizationLockException();

				// If the user is trying to release a monitor not held by this thread,
				// that's an error.
				if (ms.OwningThread != Thread.CurrentThread)
				{
					throw new SynchronizationLockException();
				}
				// If this thread has reentered this monitor, just decrement the count
				else if (ms.ReentranceCount > 0)
				{
					ms.ReentranceCount--;
				}
				// Otherwise, if this thread will now be releasing the monitor,
				// update the MonitorState accordingly.  And in addition,
				// if there are no threads waiting on this monitor, free
				// the MonitorState data by removing it from the mapping table.
				else
				{
					ms.OwningThread = null;
					if (ms.WaitingThreads.Count == 0) _monitorStates.Remove(monitor);
				}

				// Finally, exit the monitor.
				Monitor.Exit(monitor);
			}
		}

		/// <summary>Throws an exception if a deadlock would be caused by the current thread waiting on the specified lock.</summary>
		/// <param name="targetMs">The target lock data.</param>
		private static void ThrowIfDeadlockDetected(MonitorState targetMs)
		{
			// If no thread is holding the target lock, then this won't deadlock...
			if (targetMs.OwningThread == null) return;

			// For the deadlock detection algorithm, we need to know what locks are
			// currently held by which threads as well as which threads are waiting on
			// which locks. We already have this information, but we need it in a tabular
			// form for easier use and better perf.
			Dictionary<Thread, List<MonitorState>> locksHeldByThreads;
			Dictionary<MonitorState, List<Thread>> threadsWaitingOnLocks;
			CreateThreadAndLockTables(out locksHeldByThreads, out threadsWaitingOnLocks);

			// As we iterate over the wait graph, we'll need to store the list of threads still left to examine
			Queue<CycleComponentNode> threadsToFollow = new Queue<CycleComponentNode>(locksHeldByThreads.Count);

			// But rather than just storing the thread, we also store the threads in the cycle that got us to this thread.
			// The top of the stack is the actual thread to be examined.
			threadsToFollow.Enqueue(new CycleComponentNode(Thread.CurrentThread, targetMs, null));

			while (threadsToFollow.Count > 0)
			{
				// Get the next thread to examine
				CycleComponentNode currentChain = threadsToFollow.Dequeue();
				Thread currentThread = currentChain.Thread;

				// If this thread doesn't hold any locks, no point in examining it
				List<MonitorState> locksHeldByThread;
				if (!locksHeldByThreads.TryGetValue(currentThread, out locksHeldByThread)) continue;

				// For each lock it does hold, add to the thread examination list all threads
				// waiting on it.  And for each, see if it completes a cycle that results in
				// a deadlock.
				foreach (MonitorState ms in locksHeldByThread)
				{
					List<Thread> nextThreads;
					if (!threadsWaitingOnLocks.TryGetValue(ms, out nextThreads)) continue;
					foreach (Thread nextThread in nextThreads)
					{
						// If any thread waiting on this lock is in the current stack,
						// it's completng a cycle... deadlock!
						if (currentChain.ContainsThread(nextThread)) throw new SynchronizationLockException(
							CreateDeadlockDescription(targetMs, currentChain, locksHeldByThreads));

						// Clone the stack of threads in the possible cycle and add this to the top,
						// then queue the stack for examination.
						threadsToFollow.Enqueue(new CycleComponentNode(nextThread, ms, currentChain));
					}
				}
			}
		}

		/// <summary>Creates a textual description of the deadlock.</summary>
		/// <param name="currentChain">The deadlock cycle.</param>
		/// <param name="locksHeldByThreads">The table containing what locks are held by each thread holding locks.</param>
		/// <returns>The description of the deadlock.</returns>
		private static string CreateDeadlockDescription(
			MonitorState competedState,
			CycleComponentNode currentChain,
			Dictionary<Thread, List<MonitorState>> locksHeldByThreads)
		{
			StringBuilder desc = new StringBuilder();
			for (CycleComponentNode node = currentChain; node != null; node = node.Next)
			{
				desc.AppendFormat("\r\nThread {0} waiting on: {1} ({2:X})\r\nCalling from:\r\n{3}\nwhile holding:\r\n",
					node.Thread.ManagedThreadId, node.MonitorState.MonitorObject.ToString(),
					RuntimeHelpers.GetHashCode(node.MonitorState.MonitorObject),
					node.MonitorState.traces[node.Thread]);

				foreach (MonitorState ms in locksHeldByThreads[node.Thread])
					desc.AppendFormat("{0} ({1:X})\r\n{2}", ms.MonitorObject.ToString(),
						RuntimeHelpers.GetHashCode(ms.MonitorObject), ms.traces[node.Thread]);

				desc.AppendLine();
			}
			return desc.ToString();
		}

		public static Thread GetMonitorOwner(object monitorObject)
		{	//Get the state!
			Thread result;

			if (!_monitorOwners.TryGetValue(monitorObject, out result))
				return null;
			return result;
		}

		public static MonitorState GetMonitorState(object monitorObject)
		{	//Get the state!
			MonitorState result;

			if (!_monitorStates.TryGetValue(monitorObject, out result))
				return null;
			return result;
		}

		/// <summary>Generates mapping tables based on the data in _monitorStates.</summary>
		/// <param name="locksHeldByThreads">A table mapping locks to the threads that hold them.</param>
		/// <param name="threadsWaitingOnLocks">A table mapping threads to the locks they're waiting on.</param>
		public static void CreateThreadAndLockTables(
			out Dictionary<Thread, List<MonitorState>> locksHeldByThreads,
			out Dictionary<MonitorState, List<Thread>> threadsWaitingOnLocks)
		{
			// Create a table of all of the locks held by threads
			locksHeldByThreads = new Dictionary<Thread, List<MonitorState>>(_monitorStates.Values.Count);
			foreach (MonitorState ms in _monitorStates.Values)
			{
				if (ms.OwningThread != null)
				{
					List<MonitorState> locksHeldByThread;
					if (!locksHeldByThreads.TryGetValue(ms.OwningThread, out locksHeldByThread))
					{
						locksHeldByThread = new List<MonitorState>(1);
						locksHeldByThreads.Add(ms.OwningThread, locksHeldByThread);
					}
					locksHeldByThread.Add(ms);
				}
			}

			// Create a table of all threads waiting on locks
			threadsWaitingOnLocks = new Dictionary<MonitorState, List<Thread>>(_monitorStates.Values.Count);
			foreach (MonitorState ms in _monitorStates.Values)
			{
				if (ms.WaitingThreads.Count > 0)
				{
					List<Thread> threadsWaitingOnLock;
					if (!threadsWaitingOnLocks.TryGetValue(ms, out threadsWaitingOnLock))
					{
						threadsWaitingOnLock = new List<Thread>(ms.WaitingThreads.Count);
						threadsWaitingOnLocks.Add(ms, threadsWaitingOnLock);
					}
					foreach (Thread t in ms.WaitingThreads)
					{
						List<MonitorState> locksHeldByThread;
						if (!locksHeldByThreads.TryGetValue(t, out locksHeldByThread) ||
							!locksHeldByThread.Contains(ms))
						{
							threadsWaitingOnLock.Add(t);
						}
					}
				}
			}
		}

		/// <summary>Information on an underlying monitor, the thread holding it, threads waiting on it, and so forth.</summary>
		public class MonitorState
		{
			public MonitorState(object monitor) { MonitorObject = monitor; traces = new Dictionary<Thread, string>(); }
			public object MonitorObject;
			public Thread OwningThread;
			public Dictionary<Thread, string> traces;
			public int ReentranceCount;
			public List<Thread> WaitingThreads = new List<Thread>();
		}

		/// <summary>Represents a node in a possible deadlock cycle.</summary>
		private class CycleComponentNode
		{
			public CycleComponentNode(Thread thread, MonitorState ms, CycleComponentNode next)
			{
				Thread = thread;
				MonitorState = ms;
				Next = next;
			}

			public Thread Thread;
			public MonitorState MonitorState;
			public CycleComponentNode Next;

			public bool ContainsThread(Thread t)
			{
				for (CycleComponentNode node = this; node != null; node = node.Next)
				{
					if (node.Thread == t) return true;
				}
				return false;
			}
		}
	}
}