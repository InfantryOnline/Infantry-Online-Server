using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using InfLauncher.Models;

namespace InfLauncher.Protocol
{
    /// <summary>
    /// Provides asynchronous asset downloading, conforming to the protocol described in the Asset Download Protocol.
    /// </summary>
    public class AssetDownloader
    {
        /// <summary>
        /// The persistent http downloading object.
        /// </summary>
        private WebClient webClient;

        /// <summary>
        /// 
        /// </summary>
        private string baseDirectoryUrl;

        /// <summary>
        /// The name of the current file being downloaded.
        /// </summary>
        private AssetDescriptor currentDescriptor;

        /// <summary>
        /// True if an asset is currently being downloaded.
        /// </summary>
        private bool isAssetDownloading;

        /// <summary>
        /// List of assets queued for download.
        /// </summary>
        private Queue<AssetDescriptor> downloadQueue;

        /// <summary>
        /// 
        /// </summary>
        public class AssetDescriptor
        {
            internal AssetDescriptor(string name, int crcValue, string compression, long downloadSize, string md5Hash, long fileSize)
            {
                Name = name;
                CrcValue = crcValue;
                Compression = compression;
                DownloadSize = downloadSize;
                Md5Hash = md5Hash;
                FileSize = fileSize;
            }

            public string Name { get; private set; }

            public int CrcValue { get; private set; }

            public string Compression { get; private set; }

            public long DownloadSize { get; private set; }

            public string Md5Hash { get; private set; }

            public long FileSize { get; private set; }
        }

        #region Response Delegates

        /// <summary>
        /// 
        /// </summary>
        public delegate void AssetDownloadBegin(AssetDescriptor asset);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalPercentageComplete"></param>
        public delegate void AssetFileListDownloadProgressChanged(int totalPercentageComplete);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetList"></param>
        public delegate void AssetFileListDownloadCompleted(List<AssetDescriptor> assetList);

        /// <summary>
        /// 
        /// </summary>
        public delegate void AssetDownloadProgressChanged(int totalPercentageComplete);

        /// <summary>
        /// 
        /// </summary>
        public delegate void AssetDownloadCompleted(Asset asset);

        /// <summary>
        /// 
        /// </summary>
        public AssetFileListDownloadProgressChanged OnAssetFileListDownloadProgressChanged;

        /// <summary>
        /// 
        /// </summary>
        public AssetFileListDownloadCompleted OnAssetFileListDownloadCompleted;

        /// <summary>
        /// 
        /// </summary>
        public AssetDownloadBegin OnAssetDownloadBegin;

        /// <summary>
        /// 
        /// </summary>
        public AssetDownloadProgressChanged OnAssetDownloadProgressChanged;

        /// <summary>
        /// 
        /// </summary>
        public AssetDownloadCompleted OnAssetDownloadCompleted;

        #endregion

        /// <summary>
        /// Creates a new AssetDownloader object given the location of the file list that references the assets
        /// to be downloaded.
        /// </summary>
        /// <param name="baseDirectoryUrl">XML file list</param>
        public AssetDownloader(string baseDirectoryUrl)
        {
            if (baseDirectoryUrl == null)
            {
                throw new ArgumentNullException("baseDirectoryUrl");
            }

            this.baseDirectoryUrl = baseDirectoryUrl;
            isAssetDownloading = false;
            downloadQueue = new Queue<AssetDescriptor>();
        }

        /// <summary>
        /// Downloads the XML file that contains references to all the assets to be downloaded. 
        /// </summary>
        /// <param name="assetFileName"></param>
        public void DownloadAssetFileList(string assetFileName)
        {
            if (assetFileName == null)
            {
                throw new ArgumentNullException("assetFileName");
            }

            webClient = new WebClient();

            // Hook in the async delegates
            webClient.DownloadProgressChanged += AssetListProgressChanged;
            webClient.DownloadDataCompleted += AssetListDownloadCompleted;

            webClient.DownloadDataAsync(new Uri(baseDirectoryUrl + assetFileName));
        }

        public void DownloadAsset(AssetDescriptor asset)
        {
            if(asset == null)
            {
                throw new ArgumentNullException("asset");
            }

            if(isAssetDownloading)
            {
                downloadQueue.Enqueue(asset);
                return;
            }

            _DownloadAsset(asset);
        }

        private void _DownloadAsset(AssetDescriptor asset)
        {
            webClient = new WebClient();

            // Hook in the async delegates
            webClient.DownloadProgressChanged += ProgressChanged;
            webClient.DownloadDataCompleted += Completed;

            // Off we go!
            currentDescriptor = asset;

            webClient.DownloadDataAsync(new Uri(baseDirectoryUrl + asset.Name + asset.Compression));
            isAssetDownloading = true;
            OnAssetDownloadBegin(asset);
        }

        #region WebClient Asynchronous Delegate Handlers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssetListProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnAssetFileListDownloadProgressChanged(e.ProgressPercentage);
        }

        private void AssetListDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var parser = new XmlFileListParser(Encoding.UTF8.GetString(e.Result));
            OnAssetFileListDownloadCompleted(parser.FileList);
        }

        /// <summary>
        /// Signaled when a new fragment of data has been downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnAssetDownloadProgressChanged(e.ProgressPercentage);
        }

        /// <summary>
        /// Signaled when a file has been fully downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Completed(object sender, DownloadDataCompletedEventArgs e)
        {
            OnAssetDownloadCompleted(new Asset(currentDescriptor.Name, e.Result, currentDescriptor));

            isAssetDownloading = false;

            if(downloadQueue.Count != 0)
            {
                _DownloadAsset(downloadQueue.Dequeue());
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        private class XmlFileListParser
        {
            public List<AssetDescriptor> FileList { get; private set; }

            /// <summary>
            /// Creates a new FileListParser that goes through an xml-based file list and retrieves
            /// the list of asset (and their attributes) that may need to be downloaded.
            /// </summary>
            /// <param name="fileData">The xml-based file list</param>
            public XmlFileListParser(string fileData)
            {
                FileList = new List<AssetDescriptor>();

                fileData = fileData.Replace("\r\n", "\n");

                try
                {
                    using (var reader = XmlReader.Create(new StringReader(fileData)))
                    {
                        while (reader.ReadToFollowing("File"))
                        {
                            reader.MoveToFirstAttribute();
                            string name = reader.Value;

                            reader.MoveToNextAttribute();
                            int crc = int.Parse(reader.Value);

                            reader.MoveToNextAttribute();
                            string compression = reader.Value;

                            reader.MoveToNextAttribute();
                            long downloadSize = long.Parse(reader.Value);

                            reader.MoveToNextAttribute();
                            string md5hash = reader.Value;

                            reader.MoveToNextAttribute();
                            long fileSize = long.Parse(reader.Value);

                            FileList.Add(new AssetDescriptor(name, crc, compression, downloadSize, md5hash, fileSize));
                        }
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }
    }
}
