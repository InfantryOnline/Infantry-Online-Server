using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using InfLauncher.Helpers;
using InfLauncher.Models;
using InfLauncher.Protocol;
using InfLauncher.Views;

namespace InfLauncher.Controllers
{
    /// <summary>
    /// The AssetDownloadController handles the connections and views associated with downloading assets.
    /// </summary>
    public class AssetDownloadController
    {
        /// <summary>
        /// Form showing the update progress.
        /// </summary>
        private UpdaterForm form = new UpdaterForm();

        /// <summary>
        /// Asynchronously downloads the assets one by one.
        /// </summary>
        private AssetDownloader assetDownloader;

        /// <summary>
        /// List of assets that are to be downloaded.
        /// </summary>
        private List<AssetDownloader.AssetDescriptor> downloadList;

        /// <summary>
        /// Number of files that are finished downloading.
        /// </summary>
        private int numFilesDownloaded;

        /// <summary>
        /// Total number of files to download.
        /// </summary>
        private int numTotalDownloads;

        /// <summary>
        /// The default path to the game directory, unless otherwise specified.
        /// </summary>
        public static string GameDirectory = Config.GetConfig().InstallPath;

        #region Delegate Methods

        /// <summary>
        /// 
        /// </summary>
        public delegate void UpdatingFinished();

        /// <summary>
        /// 
        /// </summary>
        public UpdatingFinished OnUpdatingFinished;

        #endregion

        /// <summary>
        /// Creates a new AssetDownloadController, which monitors the updating of the game every time
        /// the launcher is started.
        /// </summary>
        /// <param name="baseUrlDirectory">The URL of the asset repository directory</param>
        public AssetDownloadController(string baseUrlDirectory)
        {
            if(baseUrlDirectory == null)
            {
                throw new ArgumentNullException("baseUrlDirectory");
            }

            downloadList = new List<AssetDownloader.AssetDescriptor>();

            numFilesDownloaded = 0;
            numTotalDownloads = 0;

            form.FormClosing += OnUpdaterFormClosing;

            assetDownloader = new AssetDownloader(baseUrlDirectory);

            assetDownloader.OnAssetFileListDownloadProgressChanged += OnAssetFileListDownloadProgressChanged;
            assetDownloader.OnAssetFileListDownloadCompleted += OnAssetFileListDownloadCompleted;

            assetDownloader.OnAssetDownloadBegin += OnAssetDownloadBegin;
            assetDownloader.OnAssetDownloadProgressChanged += OnAssetDownloadProgressChanged;
            assetDownloader.OnAssetDownloadCompleted += OnAssetDownloadCompleted;
        }

        /// <summary>
        /// Proceeds to run the installation and updating procedure.
        /// </summary>
        /// <remarks>
        /// Once the installation and updating are done, the delegate 
        /// </remarks>
        public void RunAsync()
        {
            assetDownloader.DownloadAssetFileList(Config.GetConfig().AssetsFileListUrl);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        private void DownloadAsset(AssetDownloader.AssetDescriptor asset)
        {
            assetDownloader.DownloadAsset(asset);
        }

        /// <summary>
        /// Goes through with a complete install of the game. First checks the existence of the folder,
        /// read/write permissions, and then downloads the files.
        /// </summary>
        /// <param name="assetList">List of files to download</param>
        private void NewInstallation(List<AssetDownloader.AssetDescriptor> assetList)
        {
            Directory.CreateDirectory(GameDirectory);
            //WriteRegistryKeys();

            form.Show();
            form.SetCurrentTask("Installing Infantry Online, please wait.");

            numFilesDownloaded = 0;
            numTotalDownloads = assetList.Count;
            form.SetFileCounts(0, numTotalDownloads);

            foreach(var asset in assetList)
            {
                DownloadAsset(asset);
            }

            downloadList = assetList;
        }

        private void WriteRegistryKeys()
        {
            Microsoft.Win32.RegistryKey key;

            for (int i = 0; i <= 5; i++)
            {
                key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(String.Format(@"Software\HarmlessGames\Infantry\Profile{0}\Options", i));
                key.SetValue("SDirectoryAddress", Config.GetConfig().DirectoryAddress);
                key.SetValue("SDirectoryAddressBackup", Config.GetConfig().DirectoryAddressBackup);
            }
        }

        private void UpdateAssets(List<AssetDownloader.AssetDescriptor> assetList)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += Md5BackgroundWorker;
            worker.RunWorkerCompleted += Md5BackgroundWorkerCompleted;
            worker.ProgressChanged += Md5BackgroundWorkerReportProgress;
            worker.WorkerReportsProgress = true;

            numFilesDownloaded = 0;
            numTotalDownloads = assetList.Count;

            form.Show();
            form.SetCurrentTask("Calculating checksum.");
            worker.RunWorkerAsync(assetList);
        }

        private void Md5BackgroundWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            List<AssetDownloader.AssetDescriptor> assetList = (List<AssetDownloader.AssetDescriptor>)e.Argument;
            List<AssetDownloader.AssetDescriptor> resultList = new List<AssetDownloader.AssetDescriptor>();

            // Go through each file; make sure it exists; make sure the MD5 checksum lines up; otherwise it's time for an update.););
            foreach (var asset in assetList)
            {
                string filePath = Path.Combine(GameDirectory, asset.Name);

                if (!File.Exists(filePath) || (GetMD5HashFromFile(filePath) != asset.Md5Hash && GetMD5HashFromFile(filePath) != "skip"))
                {
                    resultList.Add(asset);
                }

                worker.ReportProgress(100);
            }

            e.Result = resultList;
        }

        private void Md5BackgroundWorkerReportProgress(object sender, ProgressChangedEventArgs e)
        {
            form.SetFileCounts(++numFilesDownloaded, numTotalDownloads);
            int progress = (int) Math.Ceiling((numFilesDownloaded/(float)numTotalDownloads)*100.0f);
            form.SetProgress(progress);
        }

        private void Md5BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<AssetDownloader.AssetDescriptor> resultList = e.Result as List<AssetDownloader.AssetDescriptor>;

            if (resultList.Count == 0)
            {
                form.Hide();
                OnUpdatingFinished();
                return;
            }

            form.Show();
            form.SetCurrentTask("Updating Infantry Online, please wait.");

            numFilesDownloaded = 0;
            numTotalDownloads = resultList.Count;
            form.SetFileCounts(0, numTotalDownloads);

            foreach (var asset in resultList)
            {
                DownloadAsset(asset);
            }
        }

        private void OnUpdaterFormClosing(Object sender, FormClosingEventArgs e)
        {
            if(CloseReason.UserClosing == e.CloseReason)
            {
                Application.ExitThread();
            }
        }

        #region AssetDownloader Delegate Handlers

        private void OnAssetFileListDownloadProgressChanged(int totalPercentage)
        {
            // Not used
        }

        private void OnAssetFileListDownloadCompleted(List<AssetDownloader.AssetDescriptor> assetList)
        {
            if(!Directory.Exists(GameDirectory))
            {
                var result =
                    MessageBox.Show("Game directory could not be found. Would you like to install Infantry Online now?",
                                    "Infantry Updater", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    NewInstallation(assetList);
                }
                else
                {
                    MessageBox.Show("Run the launcher again when you wish to install the game.", "Game Updater");
                    Application.Exit();
                }
            }
            else
            {
                UpdateAssets(assetList);
            }
            //Write the registry keys mang
            WriteRegistryKeys();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        private void OnAssetDownloadBegin(AssetDownloader.AssetDescriptor asset)
        {
            form.SetFilename(asset.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalPercentage"></param>
        private void OnAssetDownloadProgressChanged(int totalPercentage)
        {
            form.SetProgress(totalPercentage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        private void OnAssetDownloadCompleted(Asset asset)
        {
            using (FileStream fs = File.Create(Path.Combine(GameDirectory, asset.FileName)))
            {
                using (MemoryStream ms = new MemoryStream(asset.Data))
                {
                    using (GZipStream decompress = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        decompress.CopyTo(fs);
                    }
                }
            }

            downloadList.Remove(asset.Descriptor);

            numFilesDownloaded++;
            form.SetFileCounts(numFilesDownloaded, numTotalDownloads);

            // Are all the files done?
            if(numFilesDownloaded == numTotalDownloads)
            {
                form.Hide();
                OnUpdatingFinished();
            }
        }

        #endregion

        private string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("X2"));
                }
                return sb.ToString();

            }
            catch (IOException) //File can't be accessed. Skip it and let Infantry.exe take care of it.
            {
                //MessageBox.Show(e.Message);
                return "skip";
            }
            catch (Exception e) //Uh oh? Tell the user whats wrong and try to continue
            {
                MessageBox.Show(e.Message);
                return "skip";
            }
        }
    }
}
