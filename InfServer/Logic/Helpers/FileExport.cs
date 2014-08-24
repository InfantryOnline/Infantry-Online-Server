using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace InfServer.Logic
{
    public partial class Logic_File
    {
        /// <summary>
        /// Creates or opens a league stat file
        /// </summary>
        /// <param name="fileName">Content File name</param>
        /// <param name="create">Create?</param>
        /// <returns>Returns the opened file</returns>
        static public FileStream CreateStatFile(string fileName)
        {
            if (!Directory.Exists(@"../Stats/"))
                Directory.CreateDirectory(@"../Stats/");

            string fullPath = Assets.AssetFileFactory.findAssetFile(fileName, @"../Stats/");
            return File.Open(fullPath, fullPath == null ? FileMode.Create : FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.ReadWrite);
        }
    }
}
