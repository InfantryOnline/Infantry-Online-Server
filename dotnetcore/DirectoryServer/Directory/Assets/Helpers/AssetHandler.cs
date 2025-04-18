using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Text;

namespace InfServer.DirectoryServer.Directory.Assets.Helpers
{
    public class AssetHandler
    {
        /// <summary>
        /// Finds the starting directory(location) within the given path
        /// </summary>
        public static string getStartingDirectory(string path, string location)
        {
            string[] curPath = path.Split('\\');
            string dir = curPath[0];
            string test = null;

            foreach(string str in curPath)
            {
                if (str == dir)
                    continue;
                dir = string.Format("{0}/{1}", dir, str);
                test = string.Format("{0}/{1}", dir, location);
                if (IO.Directory.Exists(test))
                    break;
            }

            return test;
        }

        /// <summary>
        /// Locates an asset file in the server's directory tree
        /// </summary>
        public static string findAssetFile(string filename, string path)
        {
            //Does the directory we're looking for exist?
            if (!IO.Directory.Exists(path))
                return null;

            //Does the file exist here?
            string filePath = IO.Path.Combine(path, filename);

            if (IO.File.Exists(filePath))
                return filePath;

            //Otherwise, search the inner directories
            foreach (string dir in IO.Directory.GetDirectories(path))
            {
                filePath = findAssetFile(filename, dir);
                if (filePath != null)
                    return filePath;
            }

            return null;
        }
    }
}
