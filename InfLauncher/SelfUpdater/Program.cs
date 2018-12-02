using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SelfUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = Process.GetProcessesByName("InfantryLauncher").FirstOrDefault();

            while (process != null && !process.HasExited)
            {
                // Wait for the process to terminate.
            }



            // Proceed to update.
            Console.WriteLine("Relaunching Launcher...");

            Thread.Sleep(3000);

            Process.Start("InfantryLauncher.exe", "skip-update");
        }
    }
}
