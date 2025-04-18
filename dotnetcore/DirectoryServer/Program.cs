﻿using System;
using System.Threading;

namespace InfServer.DirectoryServer
{
    class Program
    {
        public static Directory.DirectoryServer server;

        public static void onException(object o, UnhandledExceptionEventArgs e)
        {	//Talk about the exception
            using (LogAssume.Assume(server._logger))
                Log.write(TLog.Exception, "Unhandled exception:\r\n" + e.ExceptionObject.ToString());
        }

        //Called when the console window is closed with Ctrl+C
        protected static void Exit(object sender, ConsoleCancelEventArgs args)
        {
            //We'll handle this
            args.Cancel = true;

            server.shutdown();
            return;
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

            //Set a handler for if we recieve Ctrl+C or Ctrl+BREAK
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);
            

            DdMonitor.bNoSync = false;
            DdMonitor.bEnabled = true;
            DdMonitor.DefaultTimeout = -1;

            //Register our catch-all exception handler
            Thread.GetDomain().UnhandledException += onException;

            //Create a logging client for the main server thread
            LogClient handlerLogger = Log.createClient("DirectoryServerHandler");
            Log.assume(handlerLogger);

            //Allow functions to pre-register
            Directory.Registrar.register();

            //Create our zone server
            server = new Directory.DirectoryServer();

            //Initialize everything..
            if (!server.Init())
            {
                Log.write(TLog.Error, "DirectoryServer initialization failed, exiting..");
                Thread.Sleep(10000);
                return;
            }

            //Good to go!
            Log.write("Listening for requests...");
            server.Begin();
        }
    }
}
