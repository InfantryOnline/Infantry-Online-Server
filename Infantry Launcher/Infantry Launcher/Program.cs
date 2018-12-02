using System;
using System.Linq;
using System.Windows.Forms;

namespace Infantry_Launcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Bypass = AwaitResponse(args);

            MainForm main = new MainForm();
            main.BypassDownload = Bypass;

            if (main.Initiate())
            { Application.Run(main); }
            else
            { Application.Exit(); }
        }
        
        static bool AwaitResponse(string[] args)
        {
            while(System.Diagnostics.Process.GetProcesses().Any(x => x.ProcessName == "Patcher"))
            {
                System.Threading.Thread.Sleep(50);
            }

            if (args.Length > 0 && args.Contains("Bypass"))
            { return true; }

            return false;
        }

        static private bool Bypass;
    }
}
