using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace InfLauncher.Helpers
{
    /// <summary>
    /// Parses the configuration file.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The path of the configuration file.
        /// </summary>
        public static string Filename = @"config.xml";

        private static Config _config;

        public string AccountsUrl { get; private set; }

        public string AssetsUrl { get; private set; }

        public string AssetsFileListUrl { get; private set; }

        public string InstallPath { get; private set; }

        public string DirectoryAddress { get; private set; }

        public string DirectoryAddressBackup { get; private set; }

        public bool Load()
        {
            using(XmlReader reader = XmlReader.Create(new FileStream(Filename, FileMode.Open)))
            {
                // Account Server configuration
                reader.ReadToFollowing("accounts");
                reader.MoveToFirstAttribute();
                AccountsUrl = reader.Value;

                // Asset Download configuration
                reader.ReadToFollowing("assets");
                reader.MoveToFirstAttribute();
                AssetsUrl = reader.Value;
                reader.MoveToNextAttribute();
                AssetsFileListUrl = reader.Value;

                // Installation path
                reader.ReadToFollowing("install");
                reader.MoveToFirstAttribute();
                InstallPath = reader.Value;

                // Directory Address
                reader.ReadToFollowing("directory");
                reader.MoveToFirstAttribute();
                DirectoryAddress = reader.Value;
                reader.MoveToNextAttribute();
                DirectoryAddressBackup = reader.Value;
            }

            return true;
        }

        public static Config GetConfig()
        {
            if(_config == null)
            {
                _config = new Config();
                _config.Load();
            }

            return _config;
        }
    }
}
