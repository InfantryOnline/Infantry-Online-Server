using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace InfantryLauncher.Protocol
{
    public class AssetDownloader
    {
        private WebClient webClient;
        private string baseDirectoryUrl;
        private AssetDownloader.AssetDescriptor currentDescriptor;
        private bool isAssetDownloading;
        private Queue<AssetDownloader.AssetDescriptor> downloadQueue;
        public AssetDownloader.AssetFileListDownloadProgressChanged OnAssetFileListDownloadProgressChanged;
        public AssetDownloader.AssetFileListDownloadCompleted OnAssetFileListDownloadCompleted;
        public AssetDownloader.AssetDownloadBegin OnAssetDownloadBegin;
        public AssetDownloader.AssetDownloadProgressChanged OnAssetDownloadProgressChanged;
        public AssetDownloader.AssetDownloadCompleted OnAssetDownloadCompleted;

        public AssetDownloader(string baseDirectoryUrl)
        {
            if (baseDirectoryUrl == null || baseDirectoryUrl == "")
                throw new ArgumentNullException("baseDirectoryUrl");
            this.baseDirectoryUrl = baseDirectoryUrl;
            this.isAssetDownloading = false;
            this.downloadQueue = new Queue<AssetDownloader.AssetDescriptor>();
        }

        public void DownloadAssetFileList(string assetFileName)
        {
            if (assetFileName == null || assetFileName == "")
                throw new ArgumentNullException("assetFileName");
            this.webClient = new WebClient();
            this.webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(this.AssetListProgressChanged);
            this.webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(this.AssetListDownloadCompleted);
            try
            {
                this.webClient.DownloadDataAsync(new Uri(this.baseDirectoryUrl + assetFileName));
            }
            catch (Exception e)
            { int num3 = (int)MessageBox.Show(e.ToString()); }
        }

        public void DownloadAsset(AssetDownloader.AssetDescriptor asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");
            if (this.isAssetDownloading)
                this.downloadQueue.Enqueue(asset);
            else
                this._DownloadAsset(asset);
        }

        private void _DownloadAsset(AssetDownloader.AssetDescriptor asset)
        {
            this.webClient = new WebClient();
            this.webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(this.ProgressChanged);
            this.webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(this.Completed);
            this.currentDescriptor = asset;
            try
            {
                this.webClient.DownloadDataAsync(new Uri(this.baseDirectoryUrl + asset.Name + asset.Compression));
            }
            catch (Exception e)
            { int num3 = (int)MessageBox.Show(e.ToString()); }

            this.isAssetDownloading = true;
            this.OnAssetDownloadBegin(asset);
        }

        private void AssetListProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.OnAssetFileListDownloadProgressChanged(e.ProgressPercentage);
        }

        private void AssetListDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var manifestData = e.Result;
            var newHash = GetMD5HashFromBytes(manifestData);

            if (File.Exists("manifest.xml"))
            {
                var existingHash = GetMD5HashFromFile("manifest.xml");

                if (newHash == existingHash)
                {
                    this.OnAssetFileListDownloadCompleted(new List<AssetDescriptor>());
                    return;
                }
            }

            try
            {
                File.WriteAllBytes("manifest.xml", manifestData);

                this.OnAssetFileListDownloadCompleted(new AssetDownloader.XmlFileListParser(Encoding.UTF8.GetString(manifestData)).FileList);
            }
            catch (Exception)
            { Application.Exit(); }
        }

        private string GetMD5HashFromBytes(byte[] bytes)
        {
            var hash = new MD5CryptoServiceProvider().ComputeHash(bytes);

            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < hash.Length; ++index)
                stringBuilder.Append(hash[index].ToString("X2"));

            return ((object)stringBuilder).ToString();
        }

        /// <summary>
        /// Ugh, copied over from MainForm.cs
        /// </summary>
        private string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream fileStream = new FileStream(fileName, FileMode.Open);
                byte[] hash = new MD5CryptoServiceProvider().ComputeHash((Stream)fileStream);
                fileStream.Close();
                StringBuilder stringBuilder = new StringBuilder();
                for (int index = 0; index < hash.Length; ++index)
                    stringBuilder.Append(hash[index].ToString("X2"));
                return ((object)stringBuilder).ToString();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                int num = (int)MessageBox.Show(ex.Message + "\r\n" + "Check read/write access to this file.");
                return null;
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.OnAssetDownloadProgressChanged(e.ProgressPercentage);
        }

        private void Completed(object sender, DownloadDataCompletedEventArgs e)
        {
            this.OnAssetDownloadCompleted(new Asset(this.currentDescriptor.Name, e.Result, this.currentDescriptor));
            this.isAssetDownloading = false;
            if (this.downloadQueue.Count == 0)
                return;
            this._DownloadAsset(this.downloadQueue.Dequeue());
        }

        public class AssetDescriptor
        {
            public string Name { get; private set; }

            public int CrcValue { get; private set; }

            public string Compression { get; private set; }

            public long DownloadSize { get; private set; }

            public string Md5Hash { get; private set; }

            public long FileSize { get; private set; }

            internal AssetDescriptor(string name, int crcValue, string compression, long downloadSize, string md5Hash, long fileSize)
            {
                this.Name = name;
                this.CrcValue = crcValue;
                this.Compression = compression;
                this.DownloadSize = downloadSize;
                this.Md5Hash = md5Hash;
                this.FileSize = fileSize;
            }
        }

        public delegate void AssetDownloadBegin(AssetDownloader.AssetDescriptor asset);

        public delegate void AssetFileListDownloadProgressChanged(int totalPercentageComplete);

        public delegate void AssetFileListDownloadCompleted(List<AssetDownloader.AssetDescriptor> assetList);

        public delegate void AssetDownloadProgressChanged(int totalPercentageComplete);

        public delegate void AssetDownloadCompleted(Asset asset);

        private class XmlFileListParser
        {
            public List<AssetDownloader.AssetDescriptor> FileList { get; private set; }

            public XmlFileListParser(string fileData)
            {
                this.FileList = new List<AssetDownloader.AssetDescriptor>();
                fileData = fileData.Replace("\r\n", "\n");
                try
                {
                    using (XmlReader xmlReader = XmlReader.Create((TextReader)new StringReader(fileData)))
                    {
                        while (xmlReader.ReadToFollowing("File"))
                        {
                            xmlReader.MoveToFirstAttribute();
                            string name = xmlReader.Value;
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
                            this.FileList.Add(new AssetDownloader.AssetDescriptor(name, crcValue, compression, downloadSize, md5Hash, fileSize));
                        }
                    }
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
