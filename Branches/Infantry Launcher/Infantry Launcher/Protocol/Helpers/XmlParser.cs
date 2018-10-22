using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Infantry_Launcher.Protocol.Helpers
{
    internal class XmlParser
    {
        /// <summary>
        /// Our list of parsed files
        /// </summary>
        public static List<AssetDescriptor> FileList { get; private set; }

        /// <summary>
        /// Our list of failed parsing attempt files
        /// </summary>
        public static List<string> FailedFiles { get; private set; }

        /// <summary>
        /// Parses the data given and adds it to a file list for retrieval
        /// </summary>
        public static void Parse(string fileData)
        {
            if (string.IsNullOrWhiteSpace(fileData))
            { throw new ArgumentNullException("Tried parsing an empty xml file"); }

            FileList = new List<AssetDescriptor>();
            fileData = fileData.Replace("\r\n", "\n");

            using (XmlReader xmlReader = XmlReader.Create(new StringReader(fileData)))
            {
                while (xmlReader.ReadToFollowing("File"))
                {
                    xmlReader.MoveToFirstAttribute();
                    string name = xmlReader.Value;

                    try
                    {
                        xmlReader.MoveToNextAttribute();
                        int crcValue = int.Parse(xmlReader.Value);
                        xmlReader.MoveToNextAttribute();
                        string compression = xmlReader.Value;
                        xmlReader.MoveToNextAttribute();
                        long downloadSize = long.Parse(xmlReader.Value);
                        xmlReader.MoveToNextAttribute();
                        string md5Hash = xmlReader.Value;
                        xmlReader.MoveToNextAttribute();
                        long fileSize = long.Parse(xmlReader.Value);

                        FileList.Add(new AssetDescriptor(name, crcValue, compression, downloadSize, md5Hash, fileSize));
                    }
                    catch(Exception e)
                    {
                        if (FailedFiles == null)
                        { FailedFiles = new List<string>(); }

                        FailedFiles.Add(string.Format("Error in parsing Asset: {0} Error: {1}", name, e.ToString()));
                    }
                }
            }
        }
    }
}
