using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace InfXmlGenerator
{
    /// <summary>
    /// XML file model.
    /// </summary>
    class AssetFile
    {
        public string Name;
        public const int Crc = -1;
        public const string Compression = ".gz";
        public long DownloadSize;
        public string Md5;
        public long Size;

        public string CompressionTech { get { return Compression; } }
        public override string ToString()
        {
            return String.Format("Name: {0}, Size: {1}, Md5: {2}", Name, Size, Md5);
        }

        public void WriteElement(XmlWriter writer)
        {
            writer.WriteStartElement("File");
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("CRC", Crc.ToString());
            writer.WriteAttributeString("Compression", Compression);
            writer.WriteAttributeString("DownloadSize", DownloadSize.ToString());
            writer.WriteAttributeString("MD5", Md5);
            writer.WriteAttributeString("Size", Size.ToString());
            writer.WriteEndElement();
        }
    }

    class Program
    {
        public static string HashManifestFile = "zipped_hash_manifest.xml";
        public static string ManifestFile = "manifest.xml";
        public static Int64 uncompressed;
        public static Int64 compressed;
        static DirectoryInfo directory;
        static DirectoryInfo location;
        static bool deleteFilesAfter = false;

        public static List<FileInfo> filesToRecheck;

        /// <summary>
        /// Writes any error message to a file
        /// </summary>
        public static void onException(object o, UnhandledExceptionEventArgs e)
        {
            string dateTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            string unhandled = "Unhandled Exception:\r\n" + e.ExceptionObject.ToString();
            string message = string.Format("[{0}] {1}", dateTime, unhandled);

            Console.WriteLine(message);
            File.AppendAllText((directory != null ? directory.FullName : Directory.GetCurrentDirectory()) + "/errorLog.txt", message + "\r\n");
        }

        /// <summary>
        /// Writes a message to a file
        /// </summary>
        public static void Write(string msg)
        {
            string dateTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            string unhandled = "Exception Caught:\r\n" + msg.ToString();
            string message = string.Format("[{0}] {1}", dateTime, unhandled);

            Console.WriteLine(message);
            File.AppendAllText((directory != null ? directory.FullName : Directory.GetCurrentDirectory()) + "/errorLog.txt", message + "\r\n");
        }

        static void Main(string[] args)
        {
            //Get our catch all
            System.Threading.Thread.GetDomain().UnhandledException += onException;
            filesToRecheck = new List<FileInfo>();

            //Get the commands
            foreach (string arg in args)
            {
                string command = arg.Substring(0, 4);
                switch(command)
                {
                    case "dir:":
                        location = new DirectoryInfo(arg.Substring(command.Length, arg.Length - command.Length));
                        break;

                    case "del:":
                        string msg = arg.Substring(command.Length, arg.Length - command.Length);
                        if (msg.Equals("true", StringComparison.OrdinalIgnoreCase))
                            deleteFilesAfter = true;
                        break;
                }
            }

            //Should we wait for the process to end?
            Process proc = Process.GetCurrentProcess();
            Process[] procs;
            bool sayOnce = false;
            while ((procs = Process.GetProcessesByName(proc.ProcessName).ToArray()) != null)
            {
                //Are we next in line? If so, lets continue our program
                if (proc.Id == procs.ElementAt(0).Id)
                    break;

                if (sayOnce)
                    continue;

                Console.WriteLine("Waiting for other " + proc.ProcessName + " processes to end...");
                sayOnce = true;
            }

            Console.WriteLine("Searching for files...");
            directory = new DirectoryInfo(location != null ? location.FullName : Directory.GetCurrentDirectory());
            FileInfo[] files = directory.GetFiles();

            List<AssetFile> assets = new List<AssetFile>();
            List<string> errorFiles = new List<string>();

            // 1. Filter out the files
            foreach (var file in files)
            {
                if (file.Name == ManifestFile || file.Extension == ".xml" || file.Name.Contains("InfXmlGenerator.exe")
                    || file.Name.Contains("errorLog") || file.Name.Contains("compressErrors") || file.Extension == AssetFile.Compression
                    || file.Name == HashManifestFile) continue;

                // Extract the first bit of data out of this
                var asset = new AssetFile();

                uncompressed += file.Length;

                asset.Name = file.Name;
                asset.Size = file.Length;
                asset.Md5 = GetMD5HashFromFile(file.FullName);

                assets.Add(asset);
            }

            // 2. Compress files
            Console.WriteLine("Compressing files...");
            foreach (var asset in assets)
            {
                if (asset.CompressionTech != ".gz")
                {
                    errorFiles.Add(asset.Name);
                    continue;
                }

                try
                {
                    var fi = new FileInfo(Path.Combine(directory.FullName, asset.Name));
                    using (FileStream inFile = fi.OpenRead())
                    {
                        using (FileStream outFile = File.Create(fi.FullName + AssetFile.Compression))
                        {
                            using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
                            {
                                inFile.CopyTo(Compress);
                                FileInfo fout = new FileInfo(fi.FullName + AssetFile.Compression);
                                asset.DownloadSize = fout.Length;

                                compressed += fout.Length;
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Write(e.ToString());
                }
            }
            Console.WriteLine("Compression Complete.");
            Console.WriteLine("{0} bytes compressed to {1} bytes. \r\n", uncompressed, compressed);
            if (errorFiles.Count > 0)
            {
                Console.WriteLine("Errors found for {0} files, outputting to a file.", errorFiles.Count);
                using (StreamWriter f = File.CreateText(Path.Combine(directory.FullName, "compressErrors")))
                {
                    f.WriteLine("Wrong compression technique used on file(s) below.");
                    foreach (string name in errorFiles)
                    {
                        Console.WriteLine(name);
                        f.WriteLine(name);
                    }
                    f.Close();
                    f.Dispose();
                }
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);

            // 3. Generate manifest
            Console.WriteLine("Generating Manifest...");
            XmlWriter writer = XmlTextWriter.Create(Path.Combine(directory.FullName, ManifestFile), settings);

            writer.WriteStartDocument();
            writer.WriteWhitespace("\n");
            writer.WriteStartElement("FIPatcher");
            writer.WriteWhitespace("\n");
            foreach (var asset in assets)
            {
                asset.WriteElement(writer);
                writer.WriteWhitespace("\n");
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();

            // 3. Filter out the files
            files = directory.GetFiles();
            assets.Clear();
            foreach(var file in files)
            {
                if (file.Extension != AssetFile.Compression)
                    continue;

                // Extract the first bit of data out of this.
                var asset = new AssetFile();

                asset.Name = file.Name;
                asset.Size = file.Length;
                asset.Md5 = GetMD5HashFromFile(file.FullName);
                asset.DownloadSize = file.Length;

                assets.Add(asset);
            }

            XmlWriterSettings setting = new XmlWriterSettings();
            setting.Encoding = new UTF8Encoding(false);

            // 4. Generate manifest
            Console.WriteLine("Generating Zipped Manifest...");
            try
            {
                XmlWriter write = XmlTextWriter.Create(Path.Combine(directory.FullName, HashManifestFile), settings);

                write.WriteStartDocument();
                write.WriteWhitespace("\n");
                write.WriteStartElement("FIPatcher");
                write.WriteWhitespace("\n");
                foreach (var asset in assets)
                {
                    asset.WriteElement(write);
                    write.WriteWhitespace("\n");
                }
                write.WriteEndElement();
                write.WriteEndDocument();

                write.Flush();
                write.Close();
                Console.WriteLine("Manifests generated.");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            // 5. Delete files if enabled
            if (deleteFilesAfter)
            {
                Console.WriteLine("Deleting old files...");
                Array.Reverse(files);
                foreach (FileInfo file in files)
                {
                    try
                    {
                        if (file.Name == ManifestFile || file.Extension == ".xml" || file.Name.Contains("InfXmlGenerator.exe")
                            || file.Name.Contains("errorLog") || file.Name.Contains("compressErrors")
                            || file.Extension == AssetFile.Compression || file.Name == HashManifestFile) continue;
                        File.Delete(file.FullName);
                    }
                    catch (IOException)
                    {
                        //If this is thrown, usually means its being used by another process meaning another zone called this program
                        //Lets add it to the list then recheck later
                        filesToRecheck.Add(file);
                    }
                    catch (Exception e)
                    {
                        Write(e.ToString());
                    }
                }
                
                //Try again
                if (filesToRecheck.Count > 0)
                {
                    int count = filesToRecheck.Count;
                    int tries = 0;
                    while(count != 0)
                    {
                        if (tries >= 5)
                            break;

                        FileInfo[] recheckfiles = filesToRecheck.ToArray();
                        foreach(FileInfo file in recheckfiles)
                        {
                            try
                            {
                                File.Delete(file.FullName);
                                filesToRecheck.Remove(file);
                                count--;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        tries++;
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }

            Console.WriteLine("Exiting...");
            System.Threading.Thread.Sleep(5000);
        }

        private static string GetMD5HashFromFile(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("X2"));
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return sb.ToString();
        }
    }
}
