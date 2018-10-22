using System;
using System.IO;

namespace AssetFetch.Logger
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
            string path = Environment.CurrentDirectory + "/logs";
            if (!Directory.Exists(path))
            { Directory.CreateDirectory(path); }

            CurrentPath = path + "/errorLog.txt";
        }

        /// <summary>
        /// Writes a log to our file
        /// </summary>
        public static void Write(string message)
        {
            string dateTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            message = string.Format("[{0}] {1}", dateTime, message);

            Console.WriteLine(message);
            File.AppendAllText(CurrentPath, message + "\r\n");
        }
    }
}
