using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using InfLauncher.Controllers;
using InfLauncher.Views;

namespace InfLauncher
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

            var controller = new MainController();
            var form = new MainForm(controller);

            controller.Form = form;
            
            Application.Run(form);
        }
    }
}
