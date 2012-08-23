using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace InfServer
{
	// TLog Enum
	/// Specifies different levels of logging
	///////////////////////////////////////////////////////
	public enum TLog
	{
		Inane,
		Normal,
		Warning,
		Error,
		Exception,
        Security
	}

	// LogAssume
	/// Object which assumes control of the logging while active
	///////////////////////////////////////////////////////
	public class LogAssume : IDisposable
	{	//Variables
		private LogClient m_client;
		private bool m_bDisposed;
		private volatile bool m_bActive;

		// Member Functions
		///////////////////////////////////////////////////
		//Constructor
		public LogAssume(LogClient client)
		{	//Sanity check
			if (client == null)
				return;

			//Make sure this is necessary
			m_bActive = true;// !Log.isCurrent(client);

			//Assume it
			if (m_bActive)
				Log.assume(client);
			m_client = client;
		}

		//Destructor
		~LogAssume()
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
			{	//Yield the logging stack if it's active
				if (m_bActive)
					Log.yield(m_client);

				m_bDisposed = true;
			}
		}

		//Static
		static public IDisposable Assume(LogClient cli)
		{
			return new LogAssume(cli);
		}
	}

	// LogClient
	/// Represents a distinguished client that uses the logging system
	///////////////////////////////////////////////////////
	public class LogClient
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		public string m_clientname;		//The name of the client represented
		public string m_guid;			//Guid for this current client
		public bool m_bFake;			//Is the logclient fake?

		public int m_clientnum;			//The number of this client type (if there are more than one)

		public StreamWriter m_writer;	//The writer for the logfile

		//Logging Event Delegate
		public delegate void LogMessage(string message);

		//Events
		public event LogMessage eException;
		public event LogMessage eError;
		public event LogMessage eWarning;
		public event LogMessage eNormal;
		public event LogMessage eInane;
        public event LogMessage eSecurity;


		// Member Functions
		///////////////////////////////////////////////////
		//Deconstructor
		~LogClient()
		{	//Dispose of the filewriter
			try
			{
				m_writer.Close();
				m_writer.Dispose();
			}
			catch (Exception)
			{
			}
		}

		// Blackhole
		/// A blackhole from which no message will return!
		///////////////////////////////////////////////////
		public static LogClient Blackhole
		{
			get
			{	//Create a fake client
				LogClient blackhole = new LogClient();
				blackhole.m_bFake = true;

				return blackhole;
			}
		}

		// close
		/// Deactivates the client and frees resources
		///////////////////////////////////////////////////
		public void close()
		{	//Flag ourselves as no fake and dispose
			if (!m_bFake)
			{
				m_bFake = true;
				m_writer.Close();
			}
		}

		// sinkMessage
		/// Sinks a message through the event delegates
		///////////////////////////////////////////////////
		public void sinkMessage(TLog type, string message)
		{	//Switch it up
			switch (type)
			{
				case TLog.Exception:
					if (eException != null)
						eException(message);
					goto case TLog.Error;

				case TLog.Error:
					if (eError != null)
						eError(message);
					goto case TLog.Warning;

				case TLog.Warning:
					if (eWarning != null)
						eWarning(message);
					goto case TLog.Normal;

				case TLog.Normal:
					if (eNormal != null)
						eNormal(message);
					goto case TLog.Inane;

				case TLog.Inane:
					if (eInane != null)
						eInane(message);
					break;

                case TLog.Security:
                    if (eSecurity != null)
                        eSecurity(message);
                    break;
			}
		}
	}

	// logstack
	/// Represents the logging state for a single thread
	///////////////////////////////////////////////////////
	public class logstack : Stack<LogClient>
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		//Sync counters
		public long m_actualsync;	//The current sync number
		public long m_runningsync;	//The next sync to queue
	}

	// Log Class
	/// Provides functionality for logging everything to disk
	///////////////////////////////////////////////////////
	public class Log
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		//All created clients
		static List<LogClient> m_clients;

		//The threads and currently associated logging clients
		static Dictionary<int, logstack> m_threads;

		//Lock used to read/write to the threadlist
		static ReaderWriterLock m_threadlock;

		//Our log threshold - anything below this we don't show to console
		static TLog m_consoleThreshold;

		//Special logfiles
		static StreamWriter m_warning;
		static StreamWriter m_error;
		static StreamWriter m_exception;
        static StreamWriter m_security;

		//Logging Event Delegate
		public delegate void LogMessage(TLog type, LogClient client, string message);

		//Events
		public static event LogMessage eException;
		public static event LogMessage eError;
		public static event LogMessage eWarning;
		public static event LogMessage eNormal;
		public static event LogMessage eInane;
        public static event LogMessage eSecurity;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		// init
		/// Initializes the login system for this process
		///////////////////////////////////////////////////
		public static bool init()
		{	//Initialize variables
			m_threads = new Dictionary<int, logstack>();
			m_threadlock = new ReaderWriterLock();
			m_clients = new List<LogClient>();

			//Ensure the oldlogs directory exists
			if (!Directory.Exists("oldlogs"))
				Directory.CreateDirectory("oldlogs");

			//Make sure our log directory exists
			if (!Directory.Exists("logs"))
				Directory.CreateDirectory("logs");
			else
			{	//Does the directory contain anything?
				if (Directory.GetFiles("logs").Length > 0)
				{	//Directory contains old logs, we need to move them.
					//Find an empty oldlogs directory to occupy
					string oldlogpath = "oldlogs\\logs";
					int lognum = 0;

					do
					{	//Find a free logfile for this clientname
						lognum++;
					}
					while (Directory.Exists(oldlogpath + lognum) == true);

					try
					{	//Move all the old log folder
						Directory.Move("logs", oldlogpath + lognum);

						//Recreate the log folder
						Directory.CreateDirectory("logs");
					}
					catch (IOException)
					{	//Assume we failed
						Console.WriteLine("Unable to initialize logger. (Files in use?)");
						return false;
					}
				}
			}

			//Open the special logs
			m_warning = new StreamWriter("logs\\warnings.txt");
			m_error = new StreamWriter("logs\\errors.txt");
			m_exception = new StreamWriter("logs\\exceptions.txt");
            m_security = new StreamWriter("logs\\security.txt");

			//Load settings
			string threshold = "Normal";
			//if (BotHandler.m_globalconfig.Settings["consoleLogThreshold"] != null)
			//	threshold = BotHandler.m_globalconfig.Settings["consoleLogThreshold"].Value;

			switch (threshold)
			{
				case "Inane":
					m_consoleThreshold = TLog.Inane;
					break;
				case "Normal":
					m_consoleThreshold = TLog.Normal;
					break;
				case "Warning":
					m_consoleThreshold = TLog.Warning;
					break;
				case "Error":
					m_consoleThreshold = TLog.Error;
					break;
				case "Exception":
					m_consoleThreshold = TLog.Exception;
					break;
                case "Security":
                    m_consoleThreshold = TLog.Security;
                    break;
				default:
					Log.write(TLog.Warning, "Unknown threshold type: " + threshold);
					break;
			}

			//Complete
			return true;
		}

		// close
		/// Closes all open log clients
		///////////////////////////////////////////////////
		public static void close()
		{	//Close each one!
			foreach (LogClient client in m_clients)
				client.close();
		}

		// createClient
		/// Creates a login client with the given credentials
		///////////////////////////////////////////////////
		public static LogClient createClient(string clientname)
		{	//Create it
			LogClient client = new LogClient();

			//Populate the variables
			client.m_clientname = clientname;
			client.m_guid = System.Guid.NewGuid().ToString();

			//Open a logfile
			string logpath = "logs\\" + clientname;
			int clientnum = 0;

			do
			{	//Find a free logfile for this clientname
				clientnum++;
			}
			while (File.Exists(logpath + clientnum + ".txt") == true);

			//Fix up our logpath
			char[] invalid = Path.GetInvalidPathChars();

			foreach (char c in invalid)
				clientname = clientname.Replace(c.ToString(), "");

			client.m_clientnum = clientnum;

			//Attempt to open the logfile
			try
			{
				client.m_writer = new StreamWriter(logpath + clientnum + ".txt");
				client.m_writer.WriteLine("* Log opened for " + clientname + " on " + DateTime.Now.ToShortDateString());
			}
			catch (Exception ex)
			{	//Make a note
				lock (m_exception)
				{
					m_exception.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Exception while opening LogClient logfile:\n" + ex.ToString());
					m_exception.Flush();
				}
			}

			//Add it to our client list
			m_clients.Add(client);

			//Return it
			return client;
		}

		// getCurrent
		/// Gets the current log client from the top of the setack
		///////////////////////////////////////////////////
		public static LogClient getCurrent()
		{	//Enter lock
			m_threadlock.AcquireReaderLock(Timeout.Infinite);

			//Get the current logclient
			logstack clientstack = null;
			int threadid = Thread.CurrentThread.ManagedThreadId;

			try
			{	//The list may be modified, so we want to remain in sync
				if (!m_threads.TryGetValue(threadid, out clientstack))
				{	//Failed to find anything related to the thread
					//Let's assume that the stack just hasn't been created
					return null;
				}

				//Take a look
				if (clientstack.Count == 0)
					return null;
				return clientstack.Peek();
			}
			finally
			{	//Release again
				m_threadlock.ReleaseReaderLock();
			}
		}

		// assume
		/// Takes over a thread in the name of a specified logging client
		///////////////////////////////////////////////////
		public static void assume(LogClient client)
		{	//Redirect
			assume(client, Thread.CurrentThread, true, -1);
		}

		internal static void assume(LogClient client, Thread thread, bool bCautious, long sync)
		{	//Validate the logclient
			if (client == null)
			{	//Wtf?
				lock (m_error)
				{
					m_error.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Attempted to add null LogClient. (assume)");
					m_error.Flush();
				}
				return;
			}

			//Ignore fake clients
			if (client.m_bFake)
				return;

			//Enter the lock to access the threads list
			logstack clientstack = null;
			m_threadlock.AcquireReaderLock(Timeout.Infinite);

			try
			{	//Find our associated client stack
				int threadid = thread.ManagedThreadId;

				//We're modifying the list, so we need to be synced
				if (!m_threads.TryGetValue(threadid, out clientstack))
				{	//Failed to find it; let's create a new one
					LockCookie lc = new LockCookie();
					lc = m_threadlock.UpgradeToWriterLock(Timeout.Infinite);

					try
					{
						clientstack = new logstack();

						//Add it to the list
						m_threads.Add(threadid, clientstack);
					}
					finally
					{
						m_threadlock.DowngradeFromWriterLock(ref lc);
					}
				}
			}
			finally
			{
				m_threadlock.ReleaseReaderLock();
			}

			//Set our sync id
			if (sync == -1)
				sync = Interlocked.Increment(ref clientstack.m_runningsync);

			//Attempt to hold the sync object
			bool bSync = true;

			if (bCautious)
				bSync = DdMonitor.TryEnter(clientstack);
			else
				DdMonitor.Enter(clientstack);

			//If this would block, queue the request in the threadpool
			if (!bSync)
			{	//Queue!
				ThreadPool.QueueUserWorkItem(
					delegate(object state)
					{	//Reattempt the assume
						assume(client, thread, false, sync);
					}
				);

				//Done
				return;
			}

			//We need to release the sync object afterwards
			try
			{	//Wait on our sync
				if (!DdMonitor.bNoSync)
					while (sync - 1 != Interlocked.Read(ref clientstack.m_actualsync))
						Monitor.Wait(clientstack);

				//Add our client
				clientstack.Push(client);

				//Increase the count
				Interlocked.Exchange(ref clientstack.m_actualsync, sync);

				//Done.
				if (!DdMonitor.bNoSync)
					Monitor.Pulse(clientstack);
			}
			finally
			{
				DdMonitor.Exit(clientstack);
			}
		}

		// yield
		/// Yields a thread to the previous logging client
		///////////////////////////////////////////////////
		public static void yield(LogClient client)
		{	//Redirect
			yield(client, Thread.CurrentThread, true, -1);
		}

		internal static void yield(LogClient client, Thread thread, bool bCautious, long sync)
		{	//Validate the logclient
			if (client == null)
			{	//Wtf?
				lock (m_error)
				{
					m_error.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Attempted to yield null LogClient. (yield)");
					m_error.Flush();
				}
				return;
			}

			//Ignore fake clients
			if (client.m_bFake)
				return;

			//Enter the lock to access the threads list
			logstack clientstack = null;
			int threadid = thread.ManagedThreadId;

			m_threadlock.AcquireReaderLock(Timeout.Infinite);

			try
			{	//Find our associated client stack
				if (!m_threads.TryGetValue(threadid, out clientstack))
				{	//Failed to find it; let's create a new one
					LockCookie lc = new LockCookie();
					lc = m_threadlock.UpgradeToWriterLock(Timeout.Infinite);

					try
					{
						clientstack = new logstack();

						//Add it to the list
						m_threads.Add(threadid, clientstack);
					}
					finally
					{
						m_threadlock.DowngradeFromWriterLock(ref lc);
					}
				}
			}
			finally
			{
				m_threadlock.ReleaseReaderLock();
			}

			//Set our sync id
			if (sync == -1)
				sync = Interlocked.Increment(ref clientstack.m_runningsync);

			//Attempt to hold the sync object
			bool bSync = true;

			if (bCautious)
				bSync = DdMonitor.TryEnter(clientstack);
			else
				DdMonitor.Enter(clientstack);

			//If this would block, queue the request in the threadpool
			if (!bSync)
			{	//Queue!
				ThreadPool.QueueUserWorkItem(
					delegate(object state)
					{	//Reattempt the assume
						yield(client, thread, false, sync);
					}
				);

				//Done
				return;
			}

			//We need to release the sync object afterwards
			try
			{	//Wait on our sync
				if (!DdMonitor.bNoSync)
					while (sync - 1 != Interlocked.Read(ref clientstack.m_actualsync))
						Monitor.Wait(clientstack);

				//Make sure the specified client holds the stack
				LogClient stackowner = clientstack.Peek();

				if (stackowner.m_guid != client.m_guid)
				{	//You can't yield if you don't own the stack
					//Report the error and do nothing
					lock (m_warning)
					{
						m_warning.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Client attempted to yield in a thread whose logging context it didn't own.");
						m_warning.Flush();
					}
					return;
				}

				//Remove the client from the stack
				clientstack.Pop();

				//If the stack has nothing left..
				if (clientstack.Count == 0)
				{	//Remove it from the thread list
					m_threadlock.AcquireWriterLock(Timeout.Infinite);

					try
					{
						m_threads.Remove(threadid);
					}
					finally
					{
						m_threadlock.ReleaseWriterLock();
					}
				}

				//Increase the count
				Interlocked.Exchange(ref clientstack.m_actualsync, sync);

				//Done
				if (!DdMonitor.bNoSync)
					Monitor.Pulse(clientstack);
			}
			finally
			{
				DdMonitor.Exit(clientstack);
			}
		}

		// write
		/// Writes to the log at the specified level
		///////////////////////////////////////////////////
		public static void write(string Message)
		{	//Assume it to be a normal message
			write(TLog.Normal, Message);
		}

		public static void write(string Message, params object[] formats)
		{	//Assume it to be a normal message
			write(TLog.Normal, String.Format(Message, formats));
		}

		public static void write(TLog type, string Message)
		{	//Redirect
			write(type, Message, Thread.CurrentThread, true);
		}

		public static void write(TLog type, string Message, params object[] formats)
		{	//Redirect
			write(type, String.Format(Message, formats), Thread.CurrentThread, true);
		}

		internal static void write(TLog type, string Message, Thread thread, bool bCautious)
		{	//Get a reader lock
			m_threadlock.AcquireReaderLock(Timeout.Infinite);

			//We need to release the sync object afterwards
			LogClient client = null;

			try
			{	//Get the current logclient
				logstack clientstack = null;
				int threadid = thread.ManagedThreadId;

				//The list may be modified, so we want to remain in sync
				if (!m_threads.TryGetValue(threadid, out clientstack))
				{	//Failed to find it; make a note
					lock (m_warning)
					{
						m_warning.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Failed to find associated clientstack for thread. (write)");
						m_warning.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Message: " + Message);
						m_warning.Flush();
					}
					return;
				}

				//If we don't have any logclients, don't bother
				if (clientstack.Count == 0)
				{	//Notify them
					lock (m_warning)
					{
						m_warning.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][Log] Attempted to write to log when no LogClients were active.");
						m_warning.Flush();
					}
					return;
				}

				//Got it!
				client = clientstack.Peek();
			}
			finally
			{	//Release our lock
				m_threadlock.ReleaseReaderLock();
			}

			//Redirect
			write(type, Message, client, bCautious);
		}

		public static void write(TLog type, string Message, LogClient client, params object[] formats)
		{
			write(type, String.Format(Message, formats), client);
		}

		public static void write(TLog type, string Message, LogClient client)
		{
			write(type, Message, client, true);
		}

        static public List<string> readLog()
        {
            List<string> logs = new List<string>();
            FileStream security = new FileStream("logs//security.txt", FileMode.Open, FileAccess.Read, System.IO.FileShare.ReadWrite);
            
            string line;
            StreamReader reader = new StreamReader(security);
            while ((line = reader.ReadLine()) != null)
            {
                logs.Add(line);
            }
            security.Close();


            return logs;
        }

		internal static void write(TLog type, string Message, LogClient client, bool bCautious)
		{	//Ignore fake clients
			if (client == null || client.m_bFake)
				return;

			//Attempt to hold the sync object
			bool bSync = true;

			if (bCautious)
				bSync = DdMonitor.TryEnter(client);
			else
				DdMonitor.Enter(client);

			//If this would block, queue the request in the threadpool
			if (!bSync)
			{	//Queue!
				ThreadPool.QueueUserWorkItem(
					delegate(object state)
					{	//Reattempt the assume
						write(type, Message, client, false);
					}
				);

				//Done
				return;
			}

			//We need to release the sync object afterwards
			try
			{	//Handle the message type
				string typechar = " ";

				switch (type)
				{
					case TLog.Inane:
						if (type >= m_consoleThreshold)
							Console.ForegroundColor = ConsoleColor.DarkGray;
						break;
					case TLog.Normal:
						if (type >= m_consoleThreshold)
							Console.ForegroundColor = ConsoleColor.Gray;
						break;
					case TLog.Warning:
						//Write to the warning log
						lock (m_warning)
						{
							m_warning.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][" + client.m_clientname + client.m_clientnum + "] " + Message);
							m_warning.Flush();
						}

						if (type >= m_consoleThreshold)
							Console.ForegroundColor = ConsoleColor.Yellow;
						typechar = "! ";
						break;
					case TLog.Error:
						//Write to the error log
						lock (m_error)
						{
							m_error.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][" + client.m_clientname + client.m_clientnum + "] " + Message);
							m_error.Flush();
						}

						if (type >= m_consoleThreshold)
							Console.ForegroundColor = ConsoleColor.Red;
						typechar = "* ";
						break;
					case TLog.Exception:
						//Write to the exception log
						lock (m_exception)
						{
							m_exception.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][" + client.m_clientname + client.m_clientnum + "] " + Message);
							m_exception.Flush();
						}

						if (type >= m_consoleThreshold)
							Console.ForegroundColor = ConsoleColor.DarkRed;
						typechar = "* ";
						break;
                    case TLog.Security:
                        lock (m_security)
                        {
                            m_exception.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][" + client.m_clientname + client.m_clientnum + "] " + Message);
                            m_security.Flush();
                        }
                        
						if (type >= m_consoleThreshold)
							Console.ForegroundColor = ConsoleColor.White;
						typechar = "! ";
                        break;
				}

				//Write the message to the logfile
				client.m_writer.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]" + typechar + Message);
				client.m_writer.Flush();

				//Obey the console threshold
				if (type >= m_consoleThreshold)
					Console.WriteLine(Message);
			}
			finally
			{
				DdMonitor.Exit(client);
			}

			//Sink the message into the logclient handlers
			client.sinkMessage(type, Message);

			//Sink it into our own handlers
			sinkMessage(type, client, Message);
		}

		// sinkMessage
		/// Sinks a message through the event delegates
		///////////////////////////////////////////////////
		internal static void sinkMessage(TLog type, LogClient client, string message)
		{	//Switch it up
			switch (type)
			{
				case TLog.Exception:
					if (eException != null)
						eException(type, client, message);
					goto case TLog.Error;

				case TLog.Error:
					if (eError != null)
						eError(type, client, message);
					goto case TLog.Warning;

				case TLog.Warning:
					if (eWarning != null)
						eWarning(type, client, message);
					goto case TLog.Normal;

				case TLog.Normal:
					if (eNormal != null)
						eNormal(type, client, message);
					goto case TLog.Inane;

				case TLog.Inane:
					if (eInane != null)
						eInane(type, client, message);
                    break;

                case TLog.Security:
                    if (eSecurity != null)
                        eSecurity(type, client, message);
					break;
			}
		}
	}
}
