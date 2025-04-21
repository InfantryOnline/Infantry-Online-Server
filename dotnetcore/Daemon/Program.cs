using System.Diagnostics;

namespace Daemon
{
    /// <summary>
    /// TODO: Update this to use Daemon style of communication using UDS/Named Pipes.
    /// 
    /// Brief design doc:
    ///     Daemon process is started at system start up, runs at all times. (Load zones up from config, monitor for file changes).
    ///     Daemon-Controller process talks to the Daemon using IPC (named pipes). Short lived, execute command, finish.
    ///     Daemon starts, stops, talks to the zone servers as needed over the named pipes.
    ///     
    ///     - Need to take special care of redirecting I/O.
    /// </summary>
    internal class Program
    {
        static DirectoryInfo newServerDir;
        static DirectoryInfo curServerDir;
        static int processID = 0;

        static void Main(string[] args)
        {
            //Get the commands
            foreach (string arg in args)
            {
                string command = arg.Substring(0, 4);
                switch (command)
                {
                    case "-d1:":
                        newServerDir = new DirectoryInfo(arg.Substring(command.Length, arg.Length - command.Length));
                        break;
                    case "-d2:":
                        curServerDir = new DirectoryInfo(arg.Substring(command.Length, arg.Length - command.Length));
                        break;
                    case "-id:":
                        processID = Convert.ToInt32(arg.Substring(command.Length, arg.Length - command.Length));
                        break;
                }
            }

            //Should we wait for the process to end?
            if (processID > 0)
            {
                while (Process.GetProcesses().Any(x => x.Id == processID))
                {
                    Console.SetCursorPosition(0, 0);
                    Console.Write("Waiting for process (id: " + processID + ") to end...");
                }
            }

            //Process ended, continue
            CopyAll(newServerDir, curServerDir);

            //Create the new InfServer process
            ProcessStartInfo srvProcess = new ProcessStartInfo(Path.Combine(curServerDir.ToString(), "ZoneServer.exe"));
            srvProcess.WorkingDirectory = curServerDir.ToString();
            Process.Start(srvProcess);
        }

        static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it’s new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
