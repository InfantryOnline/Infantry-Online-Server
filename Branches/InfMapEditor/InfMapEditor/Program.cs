using System;
using System.Windows.Forms;
using InfMapEditor.Controllers;
using SlimDX.Windows;

namespace InfMapEditor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainController c = new MainController();
            MessagePump.Run(c, c.PostWindowsLoop);
        }
    }
}
