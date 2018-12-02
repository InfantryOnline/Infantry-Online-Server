using System;
using System.Threading;

using AssetFetch.Logger;

namespace AssetFetch
{
    class Program
    {
        public static bool running = false;
        /// <summary>
        /// Exits our application
        /// </summary>
        public static void Exit()
        {
            System.Diagnostics.Process.GetCurrentProcess().Close();
        }

        /// <summary>
        /// Writes any error message to a file
        /// </summary>
        public static void onException(object o, UnhandledExceptionEventArgs e)
        {
            Log.Write("Unhandled Exception:\r\n" + e.ExceptionObject.ToString());
        }

        static void Main(string[] args)
        {
            //Set log path first
            Log.SetLogPath();

            //Get our catch all
            Thread.GetDomain().UnhandledException += onException;

            if (!AssetController.Init())
            {
                Log.Write("Exiting....");
                Thread.Sleep(2000);
                return;
            }

            AssetController.Begin();
            while(running) { }

            Exit();
        }
        
    }
}
