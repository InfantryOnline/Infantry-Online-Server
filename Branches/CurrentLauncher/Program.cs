using InfantryLauncher.Forms;
using System;
using System.Windows.Forms;

namespace InfantryLauncher
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm main = new MainForm();
            if (main.Initiate())
                Application.Run((Form)main);
            else
                Application.Exit();
        }
    }
}
