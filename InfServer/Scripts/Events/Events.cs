using System;
using System.Collections.Generic;

using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

using InfServer.Scripting;


namespace InfServer
{
	// EventObjects class
	/// Exposes some standard functions for event objects to use
	///////////////////////////////////////////////////////
	public class EventObjects
	{
		/// <summary>
		/// Initializes events for the event object
		/// </summary>
		static public void eventInit(IEventObject obj, bool bParseEvents)
		{	//Initialize variables
			obj.events = new EventHandlers(obj);
			obj._sync = new object();

			if (bParseEvents)
			{   //Obtain the group type
				Type type = obj.GetType();

				//Initialize all script events
				foreach (MethodInfo method in type.GetMethods())
				{	//Look at it's attributes..
					foreach (Attribute attr in method.GetCustomAttributes(true))
					{	//.. until we find an event handler attribute
						Scripts.Event ehdlr = attr as Scripts.Event;
						if (ehdlr == null)
							continue;

						//Register the event with the group eventobject
						obj.events[ehdlr.eventname] += new CEventHandler(obj, method.Name);
					}
				}
			}
		}

		/// <summary>
		/// Triggers an event
		/// </summary>
		static public void trigger(IEventObject obj, string name, bool bCautious, params object[] args)
		{	//Attempt to hold the sync object
			bool bSync = true;

			if (bCautious)
				bSync = DdMonitor.TryEnter(obj._sync);
			else
				DdMonitor.Enter(obj._sync);

			//If this would block, queue the request in the threadpool
			if (!bSync)
			{	//Queue!
				ThreadPool.QueueUserWorkItem(
					delegate(object state)
					{	//Reattempt the trigger
						trigger(obj, name, false, args);
					}
				);

				//Done
				return;
			}

			try
			{	//Does the event exist?
				HandlerList list;

				if (!obj.events.TryGetValue(name, out list))
					//No? No need to worry
					return;

				//Got it! Execute all handlers
				foreach (CEventHandler eh in list.methods)
				{	//Sanity checks
					if (eh.argnum != args.Length)
						throw new ArgumentException("Parameter mismatch while attempting to invoke custom event '" + name + "'");

					//Use the logger if necessary
					using (LogAssume.Assume(obj._eventLogger))
					{	//We're ok, time to invoke it
						//Custom handler?
						if (eh.customcaller == null)
							eh.handler(eh.that, args);
						else
							//Allow them to call it
							eh.customcaller(eh, false, eh.that, args);
					}
				}
			}
			catch (Exception ex)
			{	//Log
				Log.write(TLog.Exception, "Exception occured while triggering event (" + name + "):\r\n" + ex.ToString());
			}
			finally
			{
				DdMonitor.Exit(obj._sync);
			}
		}

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		static public object callsync(IEventObject obj, string name, bool bSync, params object[] args)
		{	//Play safe?
			if (bSync)
			{
				using (DdMonitor.Lock(obj._sync))
				{	//Does the event exist?
					HandlerList list;

					if (!obj.events.TryGetValue(name, out list))
						//No? No need to worry
						return null;

					//Got it! Execute all handlers
					foreach (CEventHandler eh in list.methods)
					{	//Sanity checks
						if (eh.argnum != args.Length)
							throw new ArgumentException("Parameter mismatch while attempting to invoke custom event '" + name + "'");

						//We're ok, time to invoke it
						object ret = null;

						//Use the logger if necessary
						using (LogAssume.Assume(obj._eventLogger))
						{	//Custom handler?
							if (eh.customcaller == null)
								ret = eh.handler(eh.that, args);
							else
								//Allow them to call it
								ret = eh.customcaller(eh, true, eh.that, args);

							//Got our loot, return with it
							return ret;
						}
					}

					return null;
				}
			}
			else
			{	//Does the event exist?
				HandlerList list;

				if (!obj.events.TryGetValue(name, out list))
					//No? No need to worry
					return null;

				//Got it! Execute all handlers
				foreach (CEventHandler eh in list.methods)
				{	//Sanity checks
					if (eh.argnum != args.Length)
						throw new ArgumentException("Parameter mismatch while attempting to invoke custom event '" + name + "'");

					//We're ok, time to invoke it
					object ret = null;

					//Use the logger if necessary
					using (LogAssume.Assume(obj._eventLogger))
					{	//Custom handler?
						if (eh.customcaller == null)
							ret = eh.handler(eh.that, args);
						else
							//Allow them to call it
							ret = eh.customcaller(eh, true, eh.that, args);

						//Got our loot, return with it
						return ret;
					}
				}

				return null;
			}
		}
	}

	// IEventObject Class
	/// Implements custom events for a class
	///////////////////////////////////////////////////////
	public interface IEventObject : IThreadedObject
	{	
		/// <summary>
		/// The event logger, if exists, for this class
		/// </summary>
		EventHandlers events
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes events for the event object
		/// </summary>
		void eventInit(bool bParseEvents);

		/// <summary>
		/// Triggers an event
		/// </summary>
		void trigger(string name, params object[] args);

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		object call(string name, params object[] args);

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		object callsync(string name, bool bSync, params object[] args);

		/// <summary>
		/// Determines if a event type exists
		/// </summary>
		bool exists(string name);

		/// <summary>
		/// Flushes the handlerlist - removing all handlers
		/// </summary>
		void flushEvents();
	}
}