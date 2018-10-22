using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

using Patcher.Logger;
using Patcher.Protocol.Helpers;
using Md5 = Patcher.Protocol.Helpers.Md5;

namespace Patcher.Protocol
{
    public class AssetDownloader
    {
        #region Delegates

        public event AssetDownloadBegin OnAssetDownloadBegin;
        public event AssetDownloadProgressChanged OnAssetDownloadProgressChanged;
        public event AssetDownloadCompleted OnAssetDownloadCompleted;

        public delegate void AssetDownloadBegin(string asset);
        public delegate void AssetDownloadProgressChanged(string totalCompleted);
        public delegate void AssetDownloadCompleted(string message);

        public event Md5ChecksumProgress OnMd5ChecksumProgress;
        public event Md5ChecksumCompleted OnMd5ChecksumCompleted;

        public delegate void Md5ChecksumProgress(string message, int progress);
        public delegate void Md5ChecksumCompleted();

        public event UpdateComplete OnUpdateComplete;
        public delegate void UpdateComplete();

        #endregion

        /// <summary>
        /// Gets or sets our current working directory
        /// </summary>
        public string CurrentDirectory
        { get; set; }

        /// <summary>
        /// Gets or sets the current url
        /// </summary>
        public string CurrentUrl
        { get; set; }

        /// <summary>
        /// Our object constructor
        /// </summary>
        public AssetDownloader()
        {
            downloadQueue = new Queue<AssetDescriptor>();
            isDownloading = false;
        }

        #region Asset List Downloading

        /// <summary>
        /// Starts our assets downloading sequence, starting with the manifest file
        /// </summary>
        public void DownloadAssetList(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                Log.Write("Asset file list doesn't exist.");
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(); }

                return;
            }

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += AssetFileListDownloadCompleted;

            try
            { webClient.DownloadDataAsync(new Uri(CurrentUrl + assetName)); }
            catch (Exception e)
            { 
                Log.Write(e.ToString());
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(); }
            }
        }

        private void AssetFileListDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var manifestData = e.Result;

            try
            {
                File.WriteAllBytes("manifest.xml", manifestData);
                XmlParser.Parse(Encoding.UTF8.GetString(manifestData));

                //Activate our MD5 Checksum
                StartMD5Checksum(XmlParser.FileList);
            }
            catch (Exception ex)
            { 
                Log.Write(ex.ToString());
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(); }
            }
        }

        #endregion

        #region MD5Checksum

        /// <summary>
        /// Starts our md5 checks, marks any files that need to be updated and downloads after
        /// </summary>
        private void StartMD5Checksum(List<AssetDescriptor> assetList)
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(Md5BackgroundWorker);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(Md5BackgroundWorkerReportProgress);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Md5BackgroundWorkerCompleted);
            backgroundWorker.WorkerReportsProgress = true;
            numFilesToDownload = 0;
            totalFilesToDownload = assetList.Count;

            backgroundWorker.RunWorkerAsync(assetList);
        }

        private void Md5BackgroundWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            List<AssetDescriptor> list1 = (List<AssetDescriptor>)e.Argument;
            List<AssetDescriptor> list2 = new List<AssetDescriptor>();
            foreach (AssetDescriptor assetDescriptor in list1)
            {   //If this download matches our exe, just continue
                if(assetDescriptor.Name.Equals(AppDomain.CurrentDomain.FriendlyName, StringComparison.OrdinalIgnoreCase))
                { continue; }

                string assetName = assetDescriptor.Name;
                string str = Path.Combine((assetName.EndsWith(".ico") || assetName.EndsWith(".png")) ? CurrentDirectory + "/imgs" : CurrentDirectory, assetName);
                string hash = Md5.Hash(str, true);
                if (!File.Exists(str) || hash != null && !hash.Equals(assetDescriptor.Md5Hash, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(str) && assetName.EndsWith(".png")) //We don't want to overwrite any custom graphics
                    { continue; }
                    assetFileName = assetName;
                    list2.Add(assetDescriptor);
                }
                backgroundWorker.ReportProgress(100);
            }
            e.Result = list2;
        }

        private void Md5BackgroundWorkerReportProgress(object sender, ProgressChangedEventArgs e)
        {
            ++numFilesToDownload;
            if (OnMd5ChecksumProgress != null)
            {
                string curFileCount = string.Format("{0}/{1}", numFilesToDownload.ToString(), totalFilesToDownload.ToString());
                int progress = (numFilesToDownload / totalFilesToDownload * 100);
                if (progress > 100)
                { progress = 100; }
                OnMd5ChecksumProgress(string.Format("{0}  |  {1}", curFileCount, assetFileName), progress);
            }
        }

        private void Md5BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<AssetDescriptor> list = e.Result as List<AssetDescriptor>;
            if (list.Count == 0)
            {
                //Skip updating
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(); }
            }
            else
            {
                numFilesToDownload = 1; //Start at 1 (our first file)
                totalFilesToDownload = list.Count;
                assetFileName = string.Empty;
                if (OnMd5ChecksumProgress != null)
                {
                    string curFileCount = string.Format("{0}/{1}", numFilesToDownload.ToString(), totalFilesToDownload.ToString());
                    int progress = (numFilesToDownload / totalFilesToDownload * 100);
                    if (progress > 100)
                    { progress = 100; }
                    OnMd5ChecksumProgress(string.Format("{0}  |  {1}", curFileCount, assetFileName), progress);
                }
                foreach (AssetDescriptor asset in list)
                { DownloadAsset(asset); }
            }
        }

        #endregion

        #region Asset Downloading

        /// <summary>
        /// Starts downloading each asset given and updating our controller
        /// </summary>
        private void DownloadAsset(AssetDescriptor asset)
        {
            if (asset == null)
            {
                ++numFilesToDownload;
                return;
            }

            //Are we already downloading?
            if (isDownloading)
            { //We are, queue it up
                downloadQueue.Enqueue(asset);
            }
            else
            { StartDequeuing(asset); }
        }

        private void StartDequeuing(AssetDescriptor asset)
        {
            currentAssetDescriptor = asset;
            assetFileName = asset.Name;

            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(AssetProgressChanged);
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(AssetDownloadingComplete);

            try
            { webClient.DownloadDataAsync(new Uri(CurrentUrl + asset.Name + asset.Compression)); }
            catch (Exception e)
            {
                if (errorInDownloads == null)
                    errorInDownloads = new List<string>();
                //Add it to the list to show later
                errorInDownloads.Add(string.Format("Asset: {0} Error: {1}", assetFileName, e.ToString()));
            }

            isDownloading = true;
            if (OnAssetDownloadBegin != null)
            { OnAssetDownloadBegin(asset.Name); }
        }

        private void AssetProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (OnAssetDownloadProgressChanged != null)
            { OnAssetDownloadProgressChanged(e.ProgressPercentage.ToString()); }
        }

        private void AssetDownloadingComplete(object sender, DownloadDataCompletedEventArgs e)
        {   //Did an error occur during downloading?
            if (e.Error != null || e.Result == null)
            {
                if (errorInDownloads == null)
                    errorInDownloads = new List<string>();
                errorInDownloads.Add(string.Format("Asset doesn't exist. Current: {0} Total: {1}", numFilesToDownload, totalFilesToDownload));
            }
            else
            {
                //Decompress and create the file
                DecompressFile(new Asset(currentAssetDescriptor.Name, e.Result, currentAssetDescriptor));
            }

            //Update
            isDownloading = false;

            if (OnAssetDownloadCompleted != null)
            {
                string curFileCount = string.Format("{0}/{1}", numFilesToDownload.ToString(), totalFilesToDownload.ToString());
                OnAssetDownloadCompleted(string.Format("{0}  |  {1}", curFileCount, assetFileName));
            }

            //Are we done?
            if (numFilesToDownload == totalFilesToDownload)
            {   //Show any residual error messages
                OutputErrorLog();
                if (OnUpdateComplete != null)
                { OnUpdateComplete(); }
                return;
            }

            if (downloadQueue.Count == 0)
            {   //Show any residual error messages
                OutputErrorLog();
                if (OnUpdateComplete != null)
                { OnUpdateComplete(); }
                return;
            }

            //Continue if we havent reached the end of the queue yet
            ++numFilesToDownload;
            StartDequeuing(downloadQueue.Dequeue());
        }

        private void DecompressFile(Asset asset)
        {
            try
            {
                using (FileStream fileStream = File.Create(Path.Combine(CurrentDirectory, asset.FileName)))
                {
                    using (MemoryStream memoryStream = new MemoryStream(asset.Data))
                    {
                        using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        { gzipStream.CopyTo(fileStream); }
                    }
                }
            }
            catch(Exception e)
            {
                if (errorInDownloads == null)
                    errorInDownloads = new List<string>();
                //Add it to the list to show later
                errorInDownloads.Add(string.Format("Asset: {0} Error: {1}", assetFileName, e.ToString()));
            }
        }

        #endregion

        #region Error Reporting

        private void OutputErrorLog()
        {
            if (errorInDownloads == null || errorInDownloads.Count == 0)
                return;

            Log.Write("There were errors during the download process.\r\nThese files below were not downloaded:");
            foreach (string str in errorInDownloads)
            { Log.Write(str); }
        }

        #endregion

        private Queue<AssetDescriptor> downloadQueue;
        private AssetDescriptor currentAssetDescriptor;
        private List<string> errorInDownloads;
        private string assetFileName;
        private int numFilesToDownload;
        private int totalFilesToDownload;
        private bool isDownloading;
    }
}