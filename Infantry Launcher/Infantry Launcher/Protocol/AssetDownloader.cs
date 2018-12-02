using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows.Forms;

using Infantry_Launcher.Protocol.Helpers;
using Md5 = Infantry_Launcher.Helpers.Md5;

namespace Infantry_Launcher.Protocol
{
    public class AssetDownloader
    {
        #region Delegates

        public event AssetDownloadBegin OnAssetDownloadBegin;
        public event AssetDownloadProgressChanged OnAssetDownloadProgressChanged;
        public event AssetDownloadCompleted OnAssetDownloadCompleted;

        public delegate void AssetDownloadBegin(AssetDescriptor asset, string message);
        public delegate void AssetDownloadProgressChanged(int totalCompleted);
        public delegate void AssetDownloadCompleted(string message);

        public event Md5ChecksumProgress OnMd5ChecksumProgress;
        public event Md5ChecksumCompleted OnMd5ChecksumCompleted;

        public delegate void Md5ChecksumProgress(string message, int progress);
        public delegate void Md5ChecksumCompleted(bool skip);

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
                MessageBox.Show("Cannot download updates, manifest file not provided.\n\rPlease check your .ini files.", "Error in downloading");
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(true); }

                return;
            }

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += AssetFileListDownloadCompleted;

            try
            { webClient.DownloadDataAsync(new Uri(CurrentUrl + assetName)); }
            catch(Exception e)
            {
                if (string.IsNullOrEmpty(CurrentUrl))
                { MessageBox.Show("Cannot download updates, website location not provided.\n\rPlease check your .ini files.", string.Format("Error downloading {0}", assetName)); }
                else { MessageBox.Show(e.ToString(), string.Format("Error downloading {0}", assetName)); }

                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(true); }
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
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error parsing manifest");
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(true); }
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
            {   //If this is our own application name, bypass it because the patcher will download it instead
                if (assetDescriptor.Name.Equals(AppDomain.CurrentDomain.FriendlyName, StringComparison.OrdinalIgnoreCase))
                { continue; }

                string str = Path.Combine(CurrentDirectory, assetDescriptor.Name);
                string hash = Md5.Hash(str, true);
                if (!File.Exists(str) || hash != null && !hash.Equals(assetDescriptor.Md5Hash, StringComparison.OrdinalIgnoreCase))
                {
                    assetFileName = assetDescriptor.Name;
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
                OnMd5ChecksumProgress(string.Format("{0}  |  {1}", "Checking files...", curFileCount), progress);
            }
        }

        private void Md5BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<AssetDescriptor> list = e.Result as List<AssetDescriptor>;
            if (list.Count == 0)
            {
                //Skip updating
                if (OnMd5ChecksumCompleted != null)
                { OnMd5ChecksumCompleted(true); }
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
                    OnMd5ChecksumProgress(string.Format("{0}  |  {1}", "Starting download...", curFileCount), progress);
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
                if (errorInDownloads == null)
                { errorInDownloads = new List<string>(); }
                LogWrite(string.Format("Asset doesn't exist.(Null) Current: {0} Total: {1}", numFilesToDownload.ToString(), totalFilesToDownload.ToString()));
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
            catch(Exception e)
            {
                if (errorInDownloads == null)
                { errorInDownloads = new List<string>(); }
                //Add it to the list to show later
                LogWrite(string.Format("Asset: {0} Error: {1}", assetFileName, e.ToString()));
            }

            isDownloading = true;
            if (OnAssetDownloadBegin != null)
            {
                string curFileCount = string.Format("{0}/{1}", numFilesToDownload.ToString(), totalFilesToDownload.ToString());
                OnAssetDownloadBegin(asset, curFileCount);
            }
        }

        private void AssetProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (OnAssetDownloadProgressChanged != null)
            { OnAssetDownloadProgressChanged(e.ProgressPercentage); }
        }

        private void AssetDownloadingComplete(object sender, DownloadDataCompletedEventArgs e)
        {   //Did an error occur during downloading?
            if (e.Error != null || e.Result == null)
            {
                if (errorInDownloads == null)
                { errorInDownloads = new List<string>(); }

                string error = string.Format("Asset doesn't exist. Current: {0} Total: {1}", numFilesToDownload, totalFilesToDownload);
                if (!string.IsNullOrEmpty(assetFileName))
                { error = string.Format("Asset \"{0}\" doesn't exist. Current: {1} Total: {2}", assetFileName, numFilesToDownload, totalFilesToDownload); }
                
                LogWrite(error);
                if (e.Error != null)
                { errorInDownloads.Add(e.Error.ToString()); } //Dont want to time stamp this because its a part of the same error
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
                OnAssetDownloadCompleted(string.Format("{0}  |  {1}", assetFileName, curFileCount));
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
                using (FileStream fileStream = File.Create(GetPath(asset.FileName)))
                {
                    using (MemoryStream memoryStream = new MemoryStream(asset.Data))
                    {
                        using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        { gzipStream.CopyTo(fileStream); }
                    }
                }
            }
            catch
            { }
        }

        private string GetPath(string assetName)
        {
            string curPath = CurrentDirectory;

            //Is this one of our resource files?
            if (assetName.EndsWith(".png") || assetName.EndsWith(".ico"))
            {
                curPath = Path.Combine(CurrentDirectory, "imgs");

                if (!Directory.Exists(curPath))
                { Directory.CreateDirectory(curPath); }
            }

            return Path.Combine(curPath, assetName);
        }

        #endregion

        #region Error Reporting

        /// <summary>
        /// Writes a string into our error log using a time stamp
        /// </summary>
        private void LogWrite(string message)
        {
            if (errorInDownloads == null)
            { errorInDownloads = new List<string>(); }

            string dateTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            message = string.Format("[{0}] {1}", dateTime, message);

            errorInDownloads.Add(message);
        }

        private void OutputErrorLog()
        {
            if (errorInDownloads == null || errorInDownloads.Count == 0)
            { return; }

            if (XmlParser.FailedFiles != null && XmlParser.FailedFiles.Count > 0)
            { XmlParser.FailedFiles.ForEach(delegate (string name) { errorInDownloads.Add(name); }); }

            string logPath = Path.Combine(CurrentDirectory, "logs");

            if (!Directory.Exists(logPath))
            { Directory.CreateDirectory(logPath); }

            MessageBox.Show("There were errors during the download process.\r\nAn error log \"Launcher_ErrorLog.txt\" will be created.", "Error in downloading.", MessageBoxButtons.OK, MessageBoxIcon.Error);

            try
            { File.AppendAllLines(Path.Combine(logPath, "Launcher_ErrorLog.txt"), errorInDownloads); }
            catch(Exception e)
            { MessageBox.Show(e.ToString(), "Error writing to Launcher_ErrorLog.txt"); }
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
