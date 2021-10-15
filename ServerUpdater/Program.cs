using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ServerUpdater
{
    /// <summary>
    /// Applies server updates from the `BinDirectory` to the other folders located in the `RootDirectory`.
    /// </summary>
    class Program
    {
        public static string RootDirectory = "C:\\Infantry\\Zones";

        public static string BinDirectory = "BIN";

        public static List<string> FilesToReplace = new List<string>
        {
            "InfServer.exe",
            "InfServer.exe.config",
            "Pathfinder.dll",
            "Ionic.Zlib.dll",
            "InfServer.Network.dll",
            "InfServer.DBComm.dll",
            "CSScriptLibrary.dll",
            "Assets.dll"
        };

        static void Main(string[] args)
        {
            List<byte[]> sourceChecksums = (from filename in FilesToReplace
                                      let path = Path.Combine(RootDirectory, BinDirectory, filename)
                                      select GetMd5FileHash(path)).ToList();

            // Scan through every folder, except the bin folder, and attempt to find the list of files that we need to replace.
            foreach (var dir in Directory.GetDirectories(RootDirectory).Where(d => d.Split(Path.DirectorySeparatorChar).Last().ToUpper() != BinDirectory))
            {
                for(var i = 0; i < FilesToReplace.Count; i++)
                {
                    var sourceFile = Path.Combine(RootDirectory, BinDirectory, FilesToReplace[i]);
                    var destinationFile = Path.Combine(dir, FilesToReplace[i]);

                    var info = new FileInfo(destinationFile);

                    if (File.Exists(destinationFile))
                    {
                        if (IsFileLocked(info))
                        {
                            Console.WriteLine("Encountered locked file at {0}", destinationFile);
                            continue;
                        }

                        var checksum = GetMd5FileHash(destinationFile);

                        if (sourceChecksums[i].SequenceEqual(checksum))
                        {
                            continue;
                        }
                    }

                    File.Copy(sourceFile, destinationFile, overwrite: true);
                }
            }
        }

        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            return false;
        }

        public static byte[] GetMd5FileHash(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }
    }
}
