using System;
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;


namespace InfServer
{
	// ObjectSafe Class
	/// Used to make sure ThreadedObject classes are used
	/// correctly.
	///////////////////////////////////////////////////////
	public class ObjectSafe : IDisposable
	{	//Variables
		private object m_used;	//The object we're using

		private bool m_bDisposed;
		private bool m_bLogActive;


		// Member Functions
		///////////////////////////////////////////////////
		//Constructor
		public ObjectSafe(object used)
		{	//Sanity check
			if (used == null)
				return;

			//Is the object castable as a threadedobject?
			IThreadedObject obj = used as IThreadedObject;

			if (obj != null)
			{	//Yes! Let's handle it
				if (obj._eventLogger != null)
				{	//Make sure it is necessary to log
					m_bLogActive = true;// !Log.isCurrent(obj.m_eventlogger);

					//Assume it
					if (m_bLogActive)
						Log.assume(obj._eventLogger);
				}

				//Enter the object's synchronous state
				DdMonitor.Enter(obj._sync);
			}
			else
				Log.write(TLog.Warning, "Attempted to ObjectSafe a null/Non-Threaded object.");

			m_used = used;
		}

		//Destructor
		~ObjectSafe()
		{	//Since we're in the destructor, the managed
			//resources will already be disposed
			Dispose(false);
		}

		//IDisposable implementation
		public void Dispose()
		{	//Dispose of both resources
			Dispose(true);

			//Tell the GC that the finalize process no longer needs
			//to be run for this object
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposeManagedResources)
		{	//Do we have anything to dispose?
			if (!m_bDisposed)
			{	//Is the object castable as a threadedobject?
				IThreadedObject obj = m_used as IThreadedObject;

				if (obj != null)
				{	//Yes, let's clean up
					//Yield to the logging stack if it's active
					if (m_bLogActive && obj._eventLogger != null)
						Log.yield(obj._eventLogger);

					//Exit the synchronous state
					DdMonitor.Exit(obj._sync);
				}

				m_bDisposed = true;
			}
		}
	}

	// IThreadedObject Class
	/// Provides easy abstraction of logging and synchronizing for classes.
	///////////////////////////////////////////////////////
	public interface IThreadedObject
	{	
		/// <summary>
		/// The event logger, if exists, for this class
		/// </summary>
		LogClient _eventLogger
		{
			get;
			set;
		}

		/// <summary>
		/// The sync object for this class
		/// </summary>
		object _sync
		{
			get;
			set;
		}
	}
}