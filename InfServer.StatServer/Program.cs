using System;
using System.Threading;

namespace InfServer.StatServer
{
    class Program
    {
        public static StatServer server;

        public static void onException(object o, UnhandledExceptionEventArgs e)
        {	//Talk about the exception
            using (LogAssume.Assume(server._logger))
                Log.write(TLog.Exception, "Unhandled exception:\r\n" + e.ExceptionObject.ToString());
        }

        static void Main(string[] args)
        {
            //Initialize the logging system
            if (!Log.init())
            {	//Abort..
                Console.WriteLine("Logger initialization failed, exiting..");
                Thread.Sleep(10000);
                return;
            }

            DdMonitor.bNoSync = false;
            DdMonitor.bEnabled = true;
            DdMonitor.DefaultTimeout = -1;

            //Register our catch-all exception handler
            Thread.GetDomain().UnhandledException += onException;

            //Create a logging client for the main server thread
            LogClient handlerLogger = Log.createClient("StatServerHandler");
            Log.assume(handlerLogger);

            //Allow functions to pre-register
            InfServer.Logic.Registrar.register();

            //Create our zone server
            server = new StatServer();

            //Initialize everything..
            if (!server.Init())
            {
                Log.write(TLog.Error, "StatServer initialization failed, exiting..");
                Thread.Sleep(10000);
                return;
            }

            //Good to go!
            Log.write("Listening for requests...");
            server.Begin();
        }
    }
}
