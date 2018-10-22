using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Patcher.Logger;

namespace Patcher
{
    class Program
    {
        static void Main(string[] args)
        {
            //Set log path first
            Log.SetLogPath();

            //Only the launcher will activate us
            if (args == null || args.Length == 0)
            {
                Log.Write("Access denied. Only the launcher can start us.");
                Thread.Sleep(1000);
                return;
            }

            //Try snagging arguments before proceeding
            string[] arguments = GetArgs(args);
            if (arguments == null)
            {
                IncompleteUpdate();
                return;
            }

            bool showOnce = false;
            while (Process.GetProcesses().Any(x => x.ProcessName == "InfantryLauncher"))
            {
                if (!showOnce)
                {
                    // Wait for the process to terminate.
                    Console.WriteLine("Waiting for our Infantry Launcher processes to end...");
                    showOnce = true;
                }

                Thread.Sleep(50);
            }

            Console.WriteLine("Starting download sequence...");
            Thread.Sleep(500);

            try
            {
                Thread _downloadThread = new Thread(o => DownloadUpdates((string[])o));
                _downloadThread.Start(arguments);
            }
            catch (Exception e)
            {
                Log.Write("Error: Cannot start downloading sequence.");
                Log.Write(e.ToString());
                IncompleteUpdate();
                return;
            }

            while (_downloading)
            { Thread.Sleep(10); }
        }

        static string[] GetArgs(string[] args)
        {
            string curDir = Directory.GetCurrentDirectory();
            string location = string.Empty;
            string manifest = string.Empty;

            //Get the commands
            foreach (string arg in args)
            {
                string command = arg.Substring(0, 9);
                switch (command)
                {
                    case "Location:":
                        location = arg.Substring(command.Length);
                        break;
                    case "Manifest:":
                        manifest = arg.Substring(command.Length);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(manifest))
            {
                Log.Write("Cannot continue with update, incorrect arguments provided.");
                Log.Write("Aborting...");
                Thread.Sleep(2000);

                _downloading = false;
                return null;
            }

            return new string[] { location, manifest };
        }

        static void DownloadUpdates(string[] args)
        {
            //Get args first
            string url = args[0];
            string manifest = args[1];

            Protocol.AssetDownloader downloader = new Protocol.AssetDownloader();

            downloader.CurrentDirectory = Directory.GetCurrentDirectory();
            downloader.CurrentUrl = url;

            //Incase the shortcut is fired within asset downloader, we immediately complete
            downloader.OnMd5ChecksumCompleted += UpdateComplete;

            downloader.OnAssetDownloadBegin += ReportProgress;
            downloader.OnAssetDownloadProgressChanged += ReportProgress;
            downloader.OnAssetDownloadCompleted += ReportProgress;

            downloader.OnUpdateComplete += UpdateComplete;

            //Start it
            downloader.DownloadAssetList(manifest);
        }

        static void ReportProgress(string msg)
        {
            Console.WriteLine(msg);
        }

        static void UpdateComplete()
        {
            Console.WriteLine("Updates completed.");

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "InfantryLauncher.exe")))
            {
                Console.WriteLine("Relaunching Launcher...");
                Thread.Sleep(3000);

                Process.Start("InfantryLauncher.exe");
            }
            _downloading = false;
        }

        static void IncompleteUpdate()
        {
            //Send bypass sequence to the launcher
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "InfantryLauncher.exe")))
            {
                Console.WriteLine("Relaunching launcher and sending a bypass signal.");
                Thread.Sleep(3000);

                Process infantry = new Process();
                infantry.StartInfo.FileName = "InfantryLauncher.exe";
                infantry.StartInfo.Arguments = "Bypass";
                infantry.Start();
            }
            _downloading = false;
        }

        private static bool _downloading = true;
    }
}