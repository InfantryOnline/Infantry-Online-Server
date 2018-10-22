using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

using AssetFetch.Logger;
using AssetFetch.Protocol.Helpers;
using Md5 = AssetFetch.Helpers.Md5;

namespace AssetFetch.Protocol
{
    public class AssetDownloader
    {
        #region Delegates

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
        /// Sends a ping request to our account server
        /// </summary>
        public static IStatus.PingRequestStatusCode PingAccount(string pingUrl)
        {
            //Since we are pinging, lets strip out our request string
            string request = "assetRequest";
            string ping = pingUrl;
            if (ping.Contains(request))
            {
                int index = ping.IndexOf(request);
                ping = pingUrl.Remove(index, pingUrl.Length - index);
            }

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(ping);
            httpWebRequest.Method = "GET";
            httpWebRequest.ContentType = "application/json";
            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    streamReader.ReadToEnd();
                    streamReader.Close();
                    return IStatus.PingRequestStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response) == null)
                { return IStatus.PingRequestStatusCode.NotFound; }

                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.NotFound:
                    default:
                        return IStatus.PingRequestStatusCode.NotFound;
                }
            }
        }

        /// <summary>
        /// Our object constructor
        /// </summary>
        public AssetDownloader()
        {
            downloadQueue = new Queue<AssetDescriptor>();
            isDownloading = false;
            parseData = false;
        }

        #region Asset List Downloading

        /// <summary>
        /// Starts our assets downloading sequence, starting with the manifest file
        /// </summary>
        public void DownloadAssetList(string assetName, bool ParseData)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                Log.Write("Cannot start downloads, manifest wasn't provided. Exiting...");
                System.Threading.Thread.Sleep(2000);
                Program.running = false;
            }

            assetFileName = assetName;
            parseData = ParseData;

            WebClient webClient = new WebClient();
            try
            {
                byte[] bytes = webClient.DownloadData(new Uri(CurrentUrl + assetName));
                AssetFileListDownloadCompleted(bytes);
            }
            catch (Exception e)
            {
                Log.Write(e.ToString());
            }
        }

        private void AssetFileListDownloadCompleted(byte[] bytes)
        {
            var manifestData = bytes;
            File.WriteAllBytes(Path.Combine(CurrentDirectory, assetFileName), manifestData);

            //Do we need to go any further?
            if (!parseData)
            {
                if (OnUpdateComplete != null)
                { OnUpdateComplete(); }
                return;
            }

            Log.Write("Parsing hash list...");
            XmlParser.Parse(Encoding.UTF8.GetString(manifestData));

            //Activate our MD5 Checksum
            StartMD5Checksum(XmlParser.FileList);
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
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Md5BackgroundWorkerCompleted);
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
            {
                string str = Path.Combine(CurrentDirectory, assetDescriptor.Name);
                string hash = Md5.Hash(str, true);
                if (!File.Exists(str) || hash != null && !hash.Equals(assetDescriptor.Md5Hash, StringComparison.OrdinalIgnoreCase))
                {
                    assetFileName = assetDescriptor.Name;
                    list2.Add(assetDescriptor);
                    ++numFilesToDownload;
                }
            }
            e.Result = list2;
        }

        private void Md5BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<AssetDescriptor> list = e.Result as List<AssetDescriptor>;
            if (list.Count == 0)
            {
                if (OnUpdateComplete != null)
                { OnUpdateComplete(); }
            }
            else
            {
                Log.Write(string.Format("Found {0} file(s) to update. Starting download...", numFilesToDownload));

                numFilesToDownload = 1;
                totalFilesToDownload = list.Count;
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
                Log.Write(string.Format("Asset doesn't exist. Current: {0} Total: {1}", numFilesToDownload, totalFilesToDownload));
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

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(AssetDownloadingComplete);
            try
            {
                webClient.DownloadDataAsync(new Uri(CurrentUrl + asset.Name));
            }
            catch (Exception e)
            {
                Log.Write(e.ToString());
            }

            isDownloading = true;
        }

        private void AssetDownloadingComplete(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null || e.Result == null)
            { 
                string error = string.Format("Asset doesn't exist. Current: {0} Total: {1}", numFilesToDownload, totalFilesToDownload);
                if (!string.IsNullOrEmpty(assetFileName))
                { error = string.Format("Asset \"{0}\" doesn't exist. Current: {1} Total: {2}", assetFileName, numFilesToDownload, totalFilesToDownload); }
                Log.Write(error);
                if (e.Error != null)
                { Log.Write(e.Error.ToString()); }
            }
            else
            { File.WriteAllBytes(Path.Combine(CurrentDirectory, assetFileName), e.Result); }

            //Update
            isDownloading = false;

            //Are we done?
            if (numFilesToDownload == totalFilesToDownload)
            {
                Log.Write("Downloads complete.");
                if (OnUpdateComplete != null)
                { OnUpdateComplete(); }
                return;
            }

            if (downloadQueue.Count == 0)
            {
                Log.Write("Downloads complete.");
                if (OnUpdateComplete != null)
                { OnUpdateComplete(); }
                return;
            }

            //Continue if we havent reached the end of the queue yet
            ++numFilesToDownload;
            StartDequeuing(downloadQueue.Dequeue());
        }
        #endregion

        private Queue<AssetDescriptor> downloadQueue;
        private AssetDescriptor currentAssetDescriptor;
        private string assetFileName;
        private int numFilesToDownload;
        private int totalFilesToDownload;
        private bool isDownloading;
        private bool parseData;
    }
}
