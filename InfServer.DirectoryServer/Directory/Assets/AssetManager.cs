using System;
using System.Collections.Generic;
using IO = System.IO;

using InfServer.DirectoryServer.Directory.Assets.Helpers;

namespace InfServer.DirectoryServer.Directory.Assets
{
    public class AssetManager
    {
        /// <summary>
        /// Gets our current asset directory
        /// </summary>
        public string AssetDirectory { get { return IO.Path.Combine(Environment.CurrentDirectory, folder); } }

        /// <summary>
        /// Generic Constructor
        /// </summary>
        public AssetManager(DirectoryServer server)
        {
            directoryServer = server;
        }
        
        /// <summary>
        /// Starts our asset processing
        /// </summary>
        public void StartListCreation(string data)
        {
            string dir;
            if (PopulateBloList(data, out dir))
            {
                //Since the filename is included,
                //populate strips it and sets just the dir path
                GatherAssetFiles(dir);
                CompressFiles(AssetDirectory);
            }
        }

        /// <summary>
        /// Parses the given data into a sorted list, returns true if successful
        /// and returns the location directory
        /// </summary>
        public bool PopulateBloList(string stringData, out string dirPath)
        {
            dirPath = string.Empty;
            if (string.IsNullOrWhiteSpace(stringData))
                return false;

            string[] data = stringData.Split('/');
            string location = data[0];
            string fileName = data[1];
            bool complete = false;

            dirPath = location;
            if (IO.File.Exists(stringData))
            {
                using (IO.StreamReader reader = IO.File.OpenText(stringData))
                {
                    string curLine;
                    while ((curLine = reader.ReadLine()) != null)
                    {
                        if (!directoryServer.AssetManifestList.Contains(curLine))
                            directoryServer.AssetManifestList.Add(curLine);
                    }
                    directoryServer.AssetManifestList.Sort();
                    complete = true;
                    reader.Close();
                }
            }
            return complete;
        }

        /// <summary>
        /// Gathers all the assets from a specific folder into our asset folder
        /// </summary>
        public void GatherAssetFiles(string fromDir)
        {
            if (!IO.Directory.Exists(AssetDirectory))
                IO.Directory.CreateDirectory(AssetDirectory);

            string blob = AssetHandler.getStartingDirectory(fromDir, "BLOBS");
            string asset = AssetHandler.getStartingDirectory(fromDir, "assets");
            foreach(string file in directoryServer.AssetManifestList)
            {
                //Check the inner zone asset folder first
                string filePath = AssetHandler.findAssetFile(file, asset);
                if (filePath == null)
                {
                    //Get it from the blob folder instead
                    filePath = AssetHandler.findAssetFile(file, blob);
                    if (filePath == null)
                        //Still cant find it? Just skip it
                        continue;
                }

                if (filePath != null)
                {
                    string path = IO.Path.Combine(AssetDirectory, file);
                    //Lets copy it over
                    if (IO.File.Exists(path))
                        IO.File.Delete(path);

                    IO.File.Copy(filePath, path);
                }
            }
        }

        /// <summary>
        /// Calls our compression exe to compress all the files within the directory
        /// </summary>
        public void CompressFiles(string directoryPath)
        {
            System.Diagnostics.Process compress = new System.Diagnostics.Process();
            compress.StartInfo.FileName = IO.Path.Combine(directoryPath, "InfXmlGenerator.exe");
            compress.StartInfo.Arguments = string.Format("dir:\"{0}\" del:\"{1}\"", directoryPath, "true");

            compress.Start();
        }

        /// <summary>
        /// Sends files to a client thats requested them
        /// </summary>
        public void SendFiles()
        {

        }

        private DirectoryServer directoryServer;
        private string folder = "Assets";
    }
}
