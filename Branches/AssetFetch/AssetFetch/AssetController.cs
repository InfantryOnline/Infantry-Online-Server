using AssetFetch.Config;
using AssetFetch.Logger;
using AssetFetch.Protocol;
using AssetFetch.Protocol.Helpers;

namespace AssetFetch
{
    public class AssetController
    {
        /// <summary>
        /// Gets or sets our current url
        /// </summary>
        public static string CurrentUrl;

        /// <summary>
        /// Gets or sets our location where we are storing our downloads at
        /// </summary>
        public static string DownloadLocation;

        /// <summary>
        /// Initializes our asset loader
        /// </summary>
        public static bool Init()
        {
            DownloadLocation = System.Environment.CurrentDirectory;
            ConfigSetting _config = ConfigSetting.Blank;

            Log.Write("Loading server config...");
            if (!System.IO.File.Exists(DownloadLocation + "/server.xml"))
            {
                Log.Write("Cannot find our server.xml file.");
                return false;
            }

            _config = new Xmlconfig("server.xml", false).Settings;

            CurrentUrl = _config["server/URL"].Value;
            backupUrl = _config["server/URLBackup"].Value;
            if (string.IsNullOrWhiteSpace(CurrentUrl) && string.IsNullOrWhiteSpace(backupUrl))
            {
                Log.Write("Cannot load server.xml file.");
                return false;
            }

            hashList = _config["assets/HashList"].Value;
            manifest = _config["assets/AssetList"].Value;

            //Lets try pinging the server to see if its even active
            Log.Write("Pinging server...");
            if (!PingServer(CurrentUrl))
            {
                CurrentUrl = backupUrl;

                //Failed, try backup
                if (!PingServer(CurrentUrl))
                {
                    Log.Write(string.Format("Cannot download file updates, server not responding at {0}.", CurrentUrl));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Starts our downloading process
        /// </summary>
        public static void Begin()
        {
            Program.running = true;

            Log.Write("Grabbing hash list from server...");

            downloader = new AssetDownloader();
            downloader.CurrentDirectory = DownloadLocation;

            downloader.CurrentUrl = CurrentUrl;

            downloader.OnUpdateComplete += GrabManifest;

            downloader.DownloadAssetList(hashList, true);
        }

        /// <summary>
        /// Pings the server, checking for activity
        /// </summary>
        private static bool PingServer(string Url)
        {
            IStatus.PingRequestStatusCode ping = AssetDownloader.PingAccount(Url);
            return ping == IStatus.PingRequestStatusCode.Ok ? true : false;
        }

        /// <summary>
        /// Called upon at the end of all downloads
        /// </summary>
        private static void UpdateComplete()
        {
            Log.Write("Downloads complete. Exiting...");
            System.Threading.Thread.Sleep(2000);
            Program.running = false;
        }

        /// <summary>
        /// Grabs a fresh copy of the manifest file
        /// </summary>
        private static void GrabManifest()
        {
            Log.Write("Grabbing manifest list from server...");
            downloader = new AssetDownloader();
            downloader.CurrentDirectory = DownloadLocation;

            downloader.CurrentUrl = CurrentUrl;

            downloader.OnUpdateComplete += UpdateComplete;

            downloader.DownloadAssetList(manifest, false);
        }

        private static AssetDownloader downloader;
        private static string backupUrl;
        private static string hashList;
        private static string manifest;
    }
}
