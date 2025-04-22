using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Pipes;
using Microsoft.Extensions.Configuration;

namespace DaemonConsole
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

        static BaseConfiguration baseConfiguration = new BaseConfiguration();

        static async Task<int> Main(string[] args)
        {
            //
            // Load in configuration.
            //

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            config.GetSection("Base").Bind(baseConfiguration);

            var daemonName = Path.GetFileNameWithoutExtension(baseConfiguration.SystemPaths.DaemonProcessPath);

            //
            // Setup command-line parameters (nb. this uses System.CommandLine which is in prerelease and
            // may never come out, but it seems to work just fine for our needs.
            //

            var install = new Command(
                "install",
                "Installs a new zone with the given name");

            var installArg = new Argument<string>(null, description: "Name of the zone to install");
            installArg.HelpName = "zone name";

            install.AddArgument(installArg);

            var kit = new Option<string>(new[] { "--kit", "-k" }, "Starting kit name (f.e. bhx, ctfpl, ...)");

            install.AddOption(kit);

            var start = new Command(
                "start",
                "Starts the zone server for the given zone name.");

            var startArg = new Argument<string>(null, description: "Name of the zone to start.");
            startArg.HelpName = "zone name";

            start.AddArgument(startArg);

            var stop = new Command(
                "stop",
                "Stops the zone server for the given zone name.");

            var stopArg = new Argument<string>(null, description: "Name of the zone to stop.");
            stopArg.HelpName = "zone name";

            stop.AddArgument(stopArg);

            var restart = new Command(
                "restart",
                "Restarts the zone server for the given zone name.");

            var restartArg = new Argument<string>(null, description: "Name of the zone to restart.");
            restartArg.HelpName = "zone name";

            restart.AddArgument(restartArg);

            var status = new Command(
                "status",
                "Prints out a summary of all zones with their names and statuses.");

            var killd = new Command(
                "killd",
                "Shoots the daemon until it dies.");

            install.SetHandler(async (string name, string kitName) =>
            {
                var newZonePath = Path.Combine(GetZonesFolderDirectoryPath(), name);

                if (Directory.Exists(newZonePath))
                {
                    Console.Error.WriteLine($"Error: Specified path already exists: {name}");
                    return;
                }

                Console.WriteLine($"Configuring new zone: {name}...");

                var downloader = new RepositoryDownloader(baseConfiguration);
                using var zip = await downloader.FetchLatestZoneServerRelease();

                ZipArchive kitZip = null;

                if (!string.IsNullOrWhiteSpace(kitName))
                {
                    var kitDownloader = new ZoneKitDownloader(baseConfiguration);
                    kitZip = await kitDownloader.FetchZoneKitByName(kitName);
                }

                try
                {
                    zip.ExtractToDirectory(newZonePath, false);

                    if (kitZip != null)
                    {
                        kitZip.ExtractToDirectory(newZonePath, false);
                        kitZip.Dispose();
                    }

                    Console.WriteLine($"Configuration completed.");
                    Console.WriteLine($"Zone folder location: {newZonePath}");
                    Console.WriteLine($"Start the zone by using \"start {name}\" command.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error extracting zip archive.");
                    Console.Error.WriteLine(ex.Message);
                }
            }, installArg, kit);

            start.SetHandler(async (string name) =>
            {
                var localhost = ".";

                var pipeClient = new NamedPipeClientStream(
                    localhost,
                    baseConfiguration.SystemPaths.DaemonPipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                await pipeClient.ConnectAsync();

                using (var ss = new StreamReader(pipeClient))
                {
                    var line = ss.ReadLine();
                    Console.WriteLine($"Server sent: {line}");
                }

                pipeClient.Close();

                Console.WriteLine(name);
            }, startArg);

            stop.SetHandler(async (string name) =>
            {
                Console.WriteLine(name);
            }, stopArg);

            restart.SetHandler(async (string name) =>
            {
                Console.WriteLine(name);
            }, restartArg);

            status.SetHandler(async () =>
            {
                var proc = Process.GetProcessesByName(daemonName).FirstOrDefault();
                if (proc == null)
                {
                    Console.Error.WriteLine("Error: Daemon process not found.");
                }
                else
                {
                    Console.WriteLine($"Daemon process running as {proc.ProcessName} with PID: {proc.Id}");
                }

                var zonesPath = GetZonesFolderDirectoryPath();

                if (!Directory.Exists(zonesPath))
                {
                    Console.WriteLine("No zones installed. Please use \"install [zone name]\" command to install a zone.");
                    return;
                }

                Console.WriteLine("Available Zones:");

                foreach(var dir in Directory.GetDirectories(zonesPath))
                {
                    var name = Path.GetFileName(dir);
                    Console.WriteLine($"\t{name}");
                }
            });

            killd.SetHandler(async () =>
            {
                var procs = Process.GetProcessesByName(daemonName);

                foreach (var p in procs)
                {
                    p.Kill();
                    Console.WriteLine($"Terminated {p.ProcessName} with ID: {p.Id}");
                }
            });

            var rootCommand = new RootCommand("Infantry Online Daemon");

            rootCommand.AddCommand(install);
            rootCommand.AddCommand(start);
            rootCommand.AddCommand(stop);
            rootCommand.AddCommand(restart);
            rootCommand.AddCommand(status);
            rootCommand.AddCommand(killd);

            //
            // Check if daemon process is even running at all.
            //

            var proc = Process.GetProcessesByName(daemonName).FirstOrDefault();

            if (proc == null)
            {
                Console.WriteLine("Daemon process not found, starting...");

                var psi = new ProcessStartInfo(baseConfiguration.SystemPaths.DaemonProcessPath);

                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                proc = new Process();
                proc.StartInfo = psi;

                proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                    Console.WriteLine($"[Daemon]: {e.Data}");
                };

                proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                    Console.Error.WriteLine($"[Daemon ERROR]: {e.Data}");
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // Give it a few seconds to start the Daemon up properly before handling anything else.
                await Task.Delay(5000);
            }

            return await rootCommand.InvokeAsync(args);
        }

        static string GetZonesFolderDirectoryPath()
        {
            string zonesFolderPath;

            if (Path.IsPathRooted(baseConfiguration.SystemPaths.ZonesFolderPath))
            {
                zonesFolderPath = baseConfiguration.SystemPaths.ZonesFolderPath;
            }
            else
            {
                zonesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), baseConfiguration.SystemPaths.ZonesFolderPath);
            }

            return Path.GetFullPath(zonesFolderPath);
        }

        static void Stuff(string[] args)
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
