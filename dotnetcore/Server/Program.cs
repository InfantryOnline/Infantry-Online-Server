﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace InfServer
{
    public class Program
    {
        static Game.ZoneServer server;

        /// <summary>
        /// Recycles our gameserver...
        /// </summary>
        public static void Restart()
        {
            ConfigSetting config = new Xmlconfig("server.xml", false).Settings;
            //Current process
            Process process = Process.GetCurrentProcess();

            if (config["server/copyServerFrom"].Value.Length > 0)
            {
                //We want to update InfServer before recycling
                Process recycler = new Process();
                recycler.StartInfo.FileName = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).ToString(), @"Global\InfRecycler.exe");
                recycler.StartInfo.Arguments = String.Format("-d1:\"{0}\" -d2:\"{1}\" -id:\"{2}\"", config["server/copyServerFrom"].Value, Directory.GetCurrentDirectory().ToString(), process.Id);

                //Start the recycler
                recycler.Start(); //The recycler waits for this (our) process to end before starting recycle/copy

                //Exit
                Stop();
            }
            else
            {
                Process.Start(Environment.CurrentDirectory + "/InfServer.exe");
                Stop();
            }
        }

        /// <summary>
        /// Stops our gameserver...
        /// </summary>
        public static void Stop()
        {
            server.run = false;
        }

        //Called when the console window is closed with Ctrl+C
        protected static void Exit(object sender, ConsoleCancelEventArgs args)
        {
            //We'll handle this
            args.Cancel = true;

            server.shutdown();
        }

        /// <summary>
        /// Makes a note of all unhandled exceptions
        /// </summary>
        public static void onException(object o, UnhandledExceptionEventArgs e)
        {	//Talk about the exception
            using (LogAssume.Assume(server._logger))
                Log.write(TLog.Exception, "Unhandled exception:\r\n" + e.ExceptionObject.ToString());
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Initializing logger..");

            //Initialize the logging system
            if (!Log.init())
            {	//Abort..
                Console.WriteLine("Logger initialization failed, exiting..");
                Thread.Sleep(10000);
                return;
            }

            DdMonitor.bNoSync = false;
            DdMonitor.bEnabled = false;
            DdMonitor.DefaultTimeout = -1;

            //Register our catch-all exception handler
            Thread.GetDomain().UnhandledException += onException;

            //Create a logging client for the main server thread
            LogClient handlerLogger = Log.createClient("ZoneServerHandler");
            Log.assume(handlerLogger);

            //Set a handler for if we recieve Ctrl+C or Ctrl+BREAK
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Exit);

            //Allow functions to pre-register
            Logic.Registrar.register();

            //Create our zone server
            server = new InfServer.Game.ZoneServer();

            //Initialize everything..
            Log.write("Initializing server..");
            if (!server.init())
            {
                Log.write(TLog.Error, "ZoneServer initialization failed, exiting..");
                Thread.Sleep(10000);
                return;
            }

            //Good to go!
            server.begin();
        }
    }
}