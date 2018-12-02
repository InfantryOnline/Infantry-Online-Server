using System;
using System.IO;

namespace Patcher.Logger
{
    /// <summary>
    /// Our logger class
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Gets or sets our current path to our error log
        /// </summary>
        public static string CurrentPath { get; set; }

        /// <summary>
        /// Sets the log path to write messages to
        /// </summary>
        public static void SetLogPath()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "logs");
            if (!Directory.Exists(path))
            { Directory.CreateDirectory(path); }

            CurrentPath = Path.Combine(path, "PatcherLog.txt");
        }

        /// <summary>
        /// Writes a log to our file
        /// </summary>
        public static void Write(string message)
        {
            Console.WriteLine(message);
            string dateTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            message = string.Format("[{0}] {1}", dateTime, message);

            File.AppendAllText(CurrentPath, "\r\n"+ message);
        }
    }
}
