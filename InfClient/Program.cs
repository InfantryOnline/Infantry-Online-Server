using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using InfServer;

namespace InfClient
{
    class Program
    {
        static InfClient client;
        public static void onException(object o, UnhandledExceptionEventArgs e)
        {	//Talk about the exception
            using (LogAssume.Assume(client._logger))
                Log.write(TLog.Exception, "Unhandled exception:\r\n" + e.ExceptionObject.ToString());
        }

        static void Main(string[] args)
        {
            Log.init();
            DdMonitor.bNoSync = true;
            DdMonitor.bEnabled = true;
            DdMonitor.DefaultTimeout = 2000;

            //Register our catch-all exception handler
            Thread.GetDomain().UnhandledException += onException;

            //Create a logging client for the main server thread
            LogClient handlerLogger = Log.createClient("ServerHandler");
            Log.assume(handlerLogger);

            //Allow functions to pre-register
            InfServer.Logic.Registrar.register();

            //Create our client
            client = new InfClient();

            //Connects to Cow Cloning Facility for now.
            IPEndPoint serverLoc = new IPEndPoint(IPAddress.Parse("64.34.162.172"), 1800);

            //Initialize everything..
            if (!client.init())
            {
                Log.write(TLog.Error, "Client initialization failed, exiting..");
                Thread.Sleep(10000);
                return;
            }

            //Connect
            client.connect(serverLoc);

            while (true)
            {
                string line = Console.ReadLine();
                InfClient.SendChat(line);
            }
        }
    }
}
