using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace InfServer.Logic
{
    public partial class Logic_File
    {
        /// <summary>
        /// Removes any illegal file characters from a string
        /// </summary>
        public static string RemoveIllegalCharacters(string str)
        {   //Remove characters...
            string c = str;
            Regex regex = new Regex(@"(\/|\*|\?|\||:|<|>)", RegexOptions.None);
            c = regex.Replace(c, @".");
            //Trim it
            str = c.Trim();
            //We have our new Infantry compatible string!
            return str;
        }

        /// <summary>
        /// Creates or opens a league stat file
        /// Note: This will overwrite anything in the file
        /// </summary>
        /// <returns>Returns the opened file stream</returns>
        static public StreamWriter CreateStatFile(string fileName)
        {
            string current = "Stats\\";
            if (!Directory.Exists(current))
                Directory.CreateDirectory(current);

            //Set file creating standard
            fileName = RemoveIllegalCharacters(fileName);
            if (!fileName.Contains(".txt"))
                fileName = fileName + ".txt";

            string fullPath = Assets.AssetFileFactory.findAssetFile(fileName, current);
            if (fullPath == null)
                //Doesnt exist
                fullPath = Path.Combine(current, fileName);
            return File.CreateText(fullPath);
        }

        /// <summary>
        /// Creates or opens a league stat file in a specified directory name
        /// Note: This will overwrite anything in the file
        /// </summary>
        /// <returns>Returns the opened file stream</returns>
        static public StreamWriter CreateStatFile(string fileName, string dirName)
        {
            string current = "Stats\\";
            if (!Directory.Exists(current))
                Directory.CreateDirectory(current);

            string path = Path.Combine(current, dirName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            //Set file creating standard
            fileName = RemoveIllegalCharacters(fileName);
            if (!fileName.Contains(".txt"))
                fileName = fileName + ".txt";

            string fullpath = Assets.AssetFileFactory.findAssetFile(fileName, path);
            if (fullpath == null)
                //Doesnt exist
                fullpath = Path.Combine(path, fileName);
            Console.WriteLine(fullpath);
            return File.CreateText(fullpath);
        }

        /// <summary>
        /// Opens a league stat file
        /// </summary>
        /// <returns>Returns the open stream</returns>
        static public StreamWriter OpenStatFile(string fileName)
        {
            string current = "Stats\\";
            if (!Directory.Exists(current))
                Directory.CreateDirectory(current);

            //Set file creating standard
            fileName = RemoveIllegalCharacters(fileName);
            if (!fileName.Contains(".txt"))
                fileName = fileName + ".txt";

            string fullPath = Assets.AssetFileFactory.findAssetFile(fileName, current);
            if (fullPath == null)
                //Doesnt exist
                fullPath = Path.Combine(current, fileName);
            return (fullPath == null ? File.CreateText(fullPath) : File.AppendText(fullPath));
        }

        /// <summary>
        /// Opens a league stat file in a specified directory name
        /// </summary>
        /// <returns>Returns the opened file stream</returns>
        static public StreamWriter OpenStatFile(string fileName, string dirName)
        {
            string current = "Stats\\";
            if (!Directory.Exists(current))
                Directory.CreateDirectory(current);

            string path = Path.Combine(current, dirName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            //Set file creating standard
            fileName = RemoveIllegalCharacters(fileName);
            if (!fileName.Contains(".txt"))
                fileName = fileName + ".txt";

            string fullpath = Assets.AssetFileFactory.findAssetFile(fileName, path);
            if (fullpath == null)
                //Doesnt exist
                fullpath = Path.Combine(path, fileName);
            return (fullpath == null ? File.CreateText(fullpath) : File.AppendText(fullpath));
        }
    }
}
