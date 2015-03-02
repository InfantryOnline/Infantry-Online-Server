using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace InfServer
{
	class Program
	{
		static DBServer server;

		public static void onException(object o, UnhandledExceptionEventArgs e)
		{	//Talk about the exception
			using (LogAssume.Assume(server._logger))
				Log.write(TLog.Exception, "Unhandled exception:\r\n" + e.ExceptionObject.ToString());
		}

        //Called when the console window is closed with Ctrl+C
        protected static void Exit(object sender, EventArgs args)
        {
            Log.write("Shutting down...");
            server.end();
        }

		static void Main(string[] args)
		{	//Initialize the logging system
			Log.init();
			DdMonitor.bNoSync = false;
			DdMonitor.bEnabled = true;
			DdMonitor.DefaultTimeout = -1;

			//Register our catch-all exception handler
			Thread.GetDomain().UnhandledException += onException;

            //Set a handler for if we recieve Ctrl+C or Ctrl+BREAK
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);

			//Create a logging client for the main server thread
			LogClient handlerLogger = Log.createClient("ServerHandler");
			Log.assume(handlerLogger);

			//Register all packet handlers
			Logic.Registrar.register();

			//Create our db server
			server = new DBServer();

			//Initialize everything..
			if (!server.init())
			{
				Log.write(TLog.Error, "DBServer initialization failed, exiting..");
				Thread.Sleep(10000);
				return;
			}

			//Good to go!
			Log.write("Starting database server..");
			server.begin();
		}
	}
}
