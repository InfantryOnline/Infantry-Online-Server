using System;
using System.IO;
using System.Windows.Forms;
using Infantry_Launcher.Protocol;
using Infantry_Launcher.Protocol.Helpers;

namespace Infantry_Launcher.Controllers
{
    public class AssetDownloadController
    {
        public static event UpdateLauncher OnUpdateLauncher;
        public static event UpdateFiles OnUpdateFiles;

        public delegate void UpdateFiles();
        public delegate void UpdateLauncher(bool skip);

        /// <summary>
        /// Gets or sets our current directory
        /// </summary>
        public static string CurrentDirectory { get; set; }

        /// <summary>
        /// Sets our current form to relay status messages only
        /// </summary>
        public static void SetForm(object Form)
        {
            if (form == null) { form = (MainForm)Form; }
        }

        /// <summary>
        /// Starts our downloads, starting with our asset list from a specified URL
        /// </summary>
        public static void DownloadAssets(string locationUrl, string manifestList)
        {
            //Find our state
            GetState();

            //Set our form
            GetForm();

            if (downloader == null)
            { downloader = new AssetDownloader(); }

            //Get or set our current working directory
            if (!string.IsNullOrWhiteSpace(CurrentDirectory))
            { downloader.CurrentDirectory = CurrentDirectory; }
            else
            { downloader.CurrentDirectory = Directory.GetCurrentDirectory(); }


            //Set our url location
            downloader.CurrentUrl = locationUrl;

            downloader.OnUpdateComplete += DontSkipUpdates; //When launcher files are done downloading

            downloader.OnAssetDownloadBegin += OnDownloadBegin;
            downloader.OnAssetDownloadProgressChanged += OnDownloadProgressChanged;
            downloader.OnAssetDownloadCompleted += OnDownloadCompleted;

            downloader.OnMd5ChecksumProgress += UpdateMsgAndProgressBar;
            downloader.OnMd5ChecksumCompleted += UpdateComplete;

            //Start our downloads
            UpdateMessage("Fetching manifest file...");
            downloader.DownloadAssetList(manifestList);
        }

        private static void OnDownloadBegin(AssetDescriptor asset, string msg)
        {
            UpdateMessage(string.Format("{0} | {1}", asset.Name, msg));
        }

        private static void OnDownloadProgressChanged(int totalPercent)
        {
            UpdateProgressBar(totalPercent);
        }

        private static void OnDownloadCompleted(string message)
        {
            UpdateMessage(message);
        }

        /// <summary>
        /// Updates our main form's status message
        /// </summary>
        private static void UpdateMessage(string msg)
        {
            form.UpdateStatusMsg(msg);
        }

        /// <summary>
        /// Updates our main form's progress bar
        /// </summary>
        private static void UpdateProgressBar(int progress)
        {
            form.UpdateProgressBar(progress);
        }

        /// <summary>
        /// Updates both the main form's message and progress bar
        /// </summary>
        private static void UpdateMsgAndProgressBar(string msg, int progress)
        {
            form.UpdateStatusMsg(msg);
            form.UpdateProgressBar(progress);
        }

        /// <summary>
        /// Alerts our main form that we are done and need to update
        /// </summary>
        private static void DontSkipUpdates()
        {
            UpdateComplete(false);
        }

        /// <summary>
        /// Alerts our main form that we are done updating
        /// </summary>
        private static void UpdateComplete(bool skipUpdates)
        {
            switch (downloadState)
            {
                case DownloadState.Launcher:
                    if (OnUpdateLauncher != null)
                    { OnUpdateLauncher(skipUpdates); }
                    break;

                case DownloadState.Assets:
                    if (OnUpdateFiles != null)
                    { OnUpdateFiles(); }
                    break;
            }
        }

        /// <summary>
        /// Gets our current state of updating
        /// </summary>
        private static void GetState()
        {
            if (OnUpdateFiles != null)
            { downloadState = DownloadState.Assets; } //Assume we've already checked the launcher, if not itll reset to it
            else
            { downloadState = DownloadState.Launcher; }
        }

        /// <summary>
        /// Gets and sets our current running form to send status messages
        /// </summary>
        private static void GetForm()
        {
            //Set it once so we can keep using it later
            //Note: incase the current active form is register form, we want to call its parent (mainform)
            if (form == null)
            { form = (MainForm)(Form.ActiveForm.GetType() != typeof(MainForm) ? Form.ActiveForm.ParentForm : Form.ActiveForm); }
        }

        private static AssetDownloader downloader;
        private static MainForm form;
        private static DownloadState downloadState;
        private enum DownloadState
        {
            Launcher,
            Assets,
        }
    }
}
