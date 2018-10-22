using InfantryLauncher.Classes;
using InfantryLauncher.Protocol;
using Ini;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace InfantryLauncher.Forms
{
    /// <summary>
    /// Starting point of our forms
    /// </summary>
    public class MainForm : Form
    {
        private IniFile settings;
        private string GameDirectory;
        private AssetDownloader assetDownloader;
        private List<AssetDownloader.AssetDescriptor> downloadList;
        private int numFilesDownloaded;
        private int numTotalDownloads;
        private string curFileCounts;
        private string curFileName;
        private Image imgBackground;
        private Image imgBtnOn;
        private Image imgBtnOff;
        private Label lblStatus;
        private VistaStyleProgressBar.ProgressBar progressBar;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private CheckBox chkRemember;
        private Button btnRegister;
        private LinkLabel lnkWebsite;
        private Button btnPlay;
        private string accountServer;
        private Label launcherUpdate;
        private bool updateLauncher;

        #region Main Form
        /// <summary>
        /// Generic Constructor
        /// </summary>
        public MainForm()
        {
            this.InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            updateLauncher = false;
        }

        /// <summary>
        /// Initiates our settings controller
        /// </summary>
        /// <returns>Returns true if successful</returns>
        public bool Initiate()
        {
            string settingsIni = (Path.Combine(Directory.GetCurrentDirectory(), "settings.ini"));
            string defaultIni = (Path.Combine(Directory.GetCurrentDirectory(), "default.ini"));
            if (!File.Exists(settingsIni) && !File.Exists(defaultIni))
            {
                int num = (int)MessageBox.Show("Could not load your settings file, make sure you installed the Infantry Launcher from the website at http://freeinfantry.org");
                return false;
            }

            this.settings = new IniFile(Path.Combine(Directory.GetCurrentDirectory(), "settings.ini"));
            IniFile iniFile = new IniFile(Path.Combine(Directory.GetCurrentDirectory(), "default.ini"));
            if (this.settings.Exists())
                this.settings.Load();
            else if (iniFile.Exists())
            {
                System.IO.File.Copy("default.ini", "settings.ini", false);
                this.settings.Load();
            }
            else
            {
                int num = (int)MessageBox.Show("Could not load your settings file, make sure you installed the Infantry Launcher from the website at http://freeinfantry.org");
                return false;
            }
            this.settings["Launcher"]["Version"] = Application.ProductVersion;
            this.settings.Save();
            return true;
        }

        /// <summary>
        /// Loads our images and forms our launcher
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            string imgs = (Path.Combine(Directory.GetCurrentDirectory(), "imgs"));
            if (Directory.Exists(imgs))
            {
                this.imgBackground = Image.FromFile(Path.Combine(imgs, "bg.png"), true);
                this.imgBtnOff = Image.FromFile(Path.Combine(imgs, "btnoff.png"), true);
                this.imgBtnOn = Image.FromFile(Path.Combine(imgs, "btnon.png"), true);
                this.BackgroundImage = this.imgBackground;
                this.btnPlay.BackgroundImage = this.imgBtnOff;
                this.btnRegister.BackgroundImage = this.imgBtnOff;
            }
            this.lblStatus.Text = string.Empty;
            this.AcceptButton = (IButtonControl)this.btnPlay;
            this.LoadSettings();

            if (Environment.GetCommandLineArgs().Any(x => x == "skip-update"))
            {
                OnUpdatingFinished(skipLauncher: true);
            }
            else
            {
                CheckUpdates();
                RunAsync();
            }
        }

        /// <summary>
        /// Loads our credentials if we have any saved
        /// </summary>
        private void LoadSettings()
        {
            string str1 = this.settings["Credentials"]["Username"];
            string str2 = this.settings["Credentials"]["Password"];
            ((Control)this.txtUsername).Select();
            if (str1.Length <= 0)
                return;
            this.txtUsername.Text = str1;
            if (str2.Length <= 0)
                return;
            this.txtPassword.Text = "*****";
            this.chkRemember.Checked = true;
        }

        /// <summary>
        /// Checks to see if our launcher needs to be updated
        /// </summary>
        private void CheckUpdates()
        {
            try
            {
                this.SetCurrentTask("Checking for launcher updates...");
                string str = new WebClient().DownloadString(this.settings["Launcher"]["VersionUrl"]);
                if (!(str != this.settings["Launcher"]["Version"]))
                    return;

                this.launcherUpdate.Visible = true;

                //Lets update
                this.updateLauncher = true;
                this.SetCurrentTask("Downloading...");
                this.AssetDownloadController(this.settings["Launcher"]["LauncherAssets"]);
                this.assetDownloader.DownloadAssetFileList(this.settings["Launcher"]["LauncherAssetsList"]);
            }
            catch (Exception e)
            {
                this.updateLauncher = false;
                this.launcherUpdate.Visible = false;
                int num = (int)MessageBox.Show("Cannot download launcher updates...." + "\r\n" + e.ToString());
            }
        }

        /// <summary>
        /// Initiates our infantry asset list downloading
        /// </summary>
        public void RunAsync()
        {
            this.SetCurrentTask("Updating assets...");
            this.AssetDownloadController(this.settings["Launcher"]["Assets"]);
            this.assetDownloader.DownloadAssetFileList(this.settings["Launcher"]["AssetsList"]);
        }

        private void lnkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(this.settings["Launcher"]["Website"]);
        }

        private void btnImages_MouseEnter(object sender, EventArgs e)
        {
            ((ButtonBase)sender).Image = this.imgBtnOn;
        }

        private void btnImages_MouseLeave(object sender, EventArgs e)
        {
            ((ButtonBase)sender).Image = this.imgBtnOff;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.SaveSettings();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            int num = (int)new RegisterForm().ShowDialog();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            this.accountServer = this.settings["Launcher"]["Accounts"];
            this.SetCurrentTask("Checking server status...");

            //Lets ping first
            AccountServer.PingRequestStatusCode ping = PingServer(this.settings["Launcher"]["Accounts"]);
            if (ping != AccountServer.PingRequestStatusCode.Ok)
            {
                //Try the backup
                if ((ping = PingServer(this.settings["Launcher"]["AccountsBackup"])) != AccountServer.PingRequestStatusCode.Ok)
                {
                    int num3 = (int)MessageBox.Show("Server error, could not connect. Is your firewall enabled?");
                    return;
                }
                this.accountServer = this.settings["Launcher"]["AccountsBackup"];
            }

            this.SetCurrentTask("Trying credentials...");
            AccountServer.LoginResponseObject payload;
            switch (AccountServer.LoginAccount(new AccountServer.LoginRequestObject()
            {
                Username = this.txtUsername.Text.Trim(),
                PasswordHash = this.txtPassword.Text == "*****" ? this.settings["Credentials"]["Password"] : Md5.Hash(this.txtPassword.Text.Trim())
            }, this.accountServer, out payload))
            {
                case AccountServer.LoginStatusCode.Ok:
                    this.LaunchGame(payload.TicketId.ToString(), ((object)payload.Username).ToString());
                    break;
                case AccountServer.LoginStatusCode.MalformedData:
                    string msg = "Error: malformed username/password";
                    int num1 = (int)MessageBox.Show((AccountServer.Reason != null ? "Error: " + AccountServer.Reason : msg));
                    break;
                case AccountServer.LoginStatusCode.InvalidCredentials:
                    int num2 = (int)MessageBox.Show("Invalid username/password");
                    break;
                case AccountServer.LoginStatusCode.ServerError:
                    int num3 = (int)MessageBox.Show("Server error, could not connect. Is your firewall enabled?");
                    break;
            }
        }

        private void LaunchGame(string ticketID, string accountName)
        {
            new Process()
            {
                StartInfo =
                {
                    FileName = Path.Combine(Environment.CurrentDirectory, "infantry.exe"),
                    Arguments = string.Format("/ticket:{0} /name:{1}", (object)ticketID, (object)accountName)
                }
            }.Start();
            Application.Exit();
        }

        /// <summary>
        /// Saves our credentials and needed info
        /// </summary>
        private void SaveSettings()
        {
            this.settings["Credentials"]["Username"] = this.txtUsername.Text;
            if (this.txtPassword.Text == "*****")
                this.settings["Credentials"]["Password"] = this.settings["Credentials"]["Password"];
            else
                this.settings["Credentials"]["Password"] = this.chkRemember.Checked ? Md5.Hash(this.txtPassword.Text) : string.Empty;
            this.settings.Save();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.Container != null)
                this.Container.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkRemember = new System.Windows.Forms.CheckBox();
            this.btnRegister = new System.Windows.Forms.Button();
            this.lnkWebsite = new System.Windows.Forms.LinkLabel();
            this.btnPlay = new System.Windows.Forms.Button();
            this.progressBar = new VistaStyleProgressBar.ProgressBar();
            this.launcherUpdate = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.Silver;
            this.lblStatus.Location = new System.Drawing.Point(9, 366);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(56, 13);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "";
            // 
            // txtUsername
            // 
            this.txtUsername.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtUsername.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtUsername.Location = new System.Drawing.Point(113, 143);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(153, 20);
            this.txtUsername.TabIndex = 0;
            // 
            // txtPassword
            // 
            this.txtPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPassword.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtPassword.Location = new System.Drawing.Point(113, 195);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(153, 20);
            this.txtPassword.TabIndex = 1;
            // 
            // chkRemember
            // 
            this.chkRemember.AutoSize = true;
            this.chkRemember.BackColor = System.Drawing.Color.Transparent;
            this.chkRemember.Location = new System.Drawing.Point(113, 232);
            this.chkRemember.Name = "chkRemember";
            this.chkRemember.Size = new System.Drawing.Size(15, 14);
            this.chkRemember.TabIndex = 2;
            this.chkRemember.UseVisualStyleBackColor = false;
            // 
            // btnRegister
            // 
            this.btnRegister.BackColor = System.Drawing.Color.Transparent;
            this.btnRegister.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRegister.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnRegister.FlatAppearance.BorderSize = 0;
            this.btnRegister.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnRegister.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegister.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRegister.ForeColor = System.Drawing.Color.Silver;
            this.btnRegister.Location = new System.Drawing.Point(158, 258);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(63, 45);
            this.btnRegister.TabIndex = 3;
            this.btnRegister.Text = "Sign Up";
            this.btnRegister.UseVisualStyleBackColor = false;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            this.btnRegister.MouseEnter += new System.EventHandler(this.btnImages_MouseEnter);
            this.btnRegister.MouseLeave += new System.EventHandler(this.btnImages_MouseLeave);
            // 
            // lnkWebsite
            // 
            this.lnkWebsite.ActiveLinkColor = System.Drawing.Color.White;
            this.lnkWebsite.AutoSize = true;
            this.lnkWebsite.BackColor = System.Drawing.Color.Transparent;
            this.lnkWebsite.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lnkWebsite.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.lnkWebsite.LinkColor = System.Drawing.Color.Silver;
            this.lnkWebsite.Location = new System.Drawing.Point(9, 9);
            this.lnkWebsite.Name = "lnkWebsite";
            this.lnkWebsite.Size = new System.Drawing.Size(66, 17);
            this.lnkWebsite.TabIndex = 5;
            this.lnkWebsite.TabStop = true;
            this.lnkWebsite.Text = "Website";
            this.lnkWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkWebsite_LinkClicked);
            // 
            // btnPlay
            // 
            this.btnPlay.BackColor = System.Drawing.Color.Transparent;
            this.btnPlay.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlay.Enabled = false;
            this.btnPlay.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnPlay.FlatAppearance.BorderSize = 0;
            this.btnPlay.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnPlay.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlay.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPlay.ForeColor = System.Drawing.Color.Silver;
            this.btnPlay.Location = new System.Drawing.Point(303, 369);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(63, 45);
            this.btnPlay.TabIndex = 6;
            this.btnPlay.Text = "Play";
            this.btnPlay.UseVisualStyleBackColor = false;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            this.btnPlay.MouseEnter += new System.EventHandler(this.btnImages_MouseEnter);
            this.btnPlay.MouseLeave += new System.EventHandler(this.btnImages_MouseLeave);
            // 
            // progressBar
            // 
            this.progressBar.BackColor = System.Drawing.Color.Transparent;
            this.progressBar.EndColor = System.Drawing.Color.Lime;
            this.progressBar.GlowColor = System.Drawing.Color.Transparent;
            this.progressBar.HighlightColor = System.Drawing.Color.Transparent;
            this.progressBar.Location = new System.Drawing.Point(12, 388);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(272, 22);
            this.progressBar.StartColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.progressBar.TabIndex = 4;
            // 
            // launcherUpdate
            // 
            this.launcherUpdate.AutoSize = true;
            this.launcherUpdate.BackColor = System.Drawing.Color.Transparent;
            this.launcherUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.launcherUpdate.ForeColor = System.Drawing.Color.IndianRed;
            this.launcherUpdate.Location = new System.Drawing.Point(113, 37);
            this.launcherUpdate.Name = "launcherUpdate";
            this.launcherUpdate.Size = new System.Drawing.Size(157, 13);
            this.launcherUpdate.TabIndex = 7;
            this.launcherUpdate.Text = "Your launcher is outdated.";
            this.launcherUpdate.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(378, 423);
            this.Controls.Add(this.launcherUpdate);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.lnkWebsite);
            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.chkRemember);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Infantry Online";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region Asset Downloading and Updating
        /// <summary>
        /// Initiates our asset list downloading
        /// </summary>
        private void AssetDownloadController(string baseUrlDirectory)
        {
            if (baseUrlDirectory == null)
                throw new ArgumentNullException("baseUrlDirectory");
            this.downloadList = new List<AssetDownloader.AssetDescriptor>();
            this.numFilesDownloaded = 0;
            this.numTotalDownloads = 0;
            this.assetDownloader = new AssetDownloader(baseUrlDirectory);
            this.assetDownloader.OnAssetFileListDownloadProgressChanged += new AssetDownloader.AssetFileListDownloadProgressChanged(this.OnAssetFileListDownloadProgressChanged);
            this.assetDownloader.OnAssetFileListDownloadCompleted += new AssetDownloader.AssetFileListDownloadCompleted(this.OnAssetFileListDownloadCompleted);
            this.assetDownloader.OnAssetDownloadBegin += new AssetDownloader.AssetDownloadBegin(this.OnAssetDownloadBegin);
            this.assetDownloader.OnAssetDownloadProgressChanged += new AssetDownloader.AssetDownloadProgressChanged(this.OnAssetDownloadProgressChanged);
            this.assetDownloader.OnAssetDownloadCompleted += new AssetDownloader.AssetDownloadCompleted(this.OnAssetDownloadCompleted);
        }

        /// <summary>
        /// Called when our infantry assets are done downloading
        /// </summary>
        public void OnUpdatingFinished(bool skipLauncher = false)
        {
            if (updateLauncher && !skipLauncher)
            {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, "SelfUpdater.exe")))
                {
                    Process.Start("SelfUpdater.exe");
                    Application.Exit();
                }
            }

            this.btnPlay.Enabled = true;
            this.lblStatus.Text = "Updating complete...";
        }

        private void SetFileCounts(int p, int numTotalDownloads)
        {
            this.curFileCounts = string.Format("{0}/{1}", (object)p.ToString(), (object)numTotalDownloads.ToString());
            this.lblStatus.Text = string.Format("{0}  |  {1}", (object)this.curFileCounts, (object)this.curFileName);
        }

        private void SetCurrentTask(string p)
        {
            this.lblStatus.Text = p;
        }

        private void SetFilename(string p)
        {
            this.curFileName = p;
            this.lblStatus.Text = string.Format("{0}  |  {1}", (object)this.curFileCounts, (object)this.curFileName);
        }

        private void SetProgress(int totalPercentage)
        {
            this.progressBar.Value = totalPercentage;
        }

        private void DownloadAsset(AssetDownloader.AssetDescriptor asset)
        {
            this.assetDownloader.DownloadAsset(asset);
        }

        private void UpdateAssets(List<AssetDownloader.AssetDescriptor> assetList)
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(this.Md5BackgroundWorker);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(this.Md5BackgroundWorkerReportProgress);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.Md5BackgroundWorkerCompleted);
            backgroundWorker.WorkerReportsProgress = true;
            this.numFilesDownloaded = 0;
            this.numTotalDownloads = assetList.Count;
            this.SetCurrentTask("Calculating checksum...");
            backgroundWorker.RunWorkerAsync((object)assetList);
        }

        private void Md5BackgroundWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            List<AssetDownloader.AssetDescriptor> list1 = (List<AssetDownloader.AssetDescriptor>)e.Argument;
            List<AssetDownloader.AssetDescriptor> list2 = new List<AssetDownloader.AssetDescriptor>();
            foreach (AssetDownloader.AssetDescriptor assetDescriptor in list1)
            {
                string str = Path.Combine(this.GameDirectory, assetDescriptor.Name);
                if (!System.IO.File.Exists(str) || this.GetMD5HashFromFile(str) != assetDescriptor.Md5Hash && this.GetMD5HashFromFile(str) != "skip")
                    list2.Add(assetDescriptor);
                backgroundWorker.ReportProgress(100);
            }
            e.Result = (object)list2;
        }

        private void Md5BackgroundWorkerReportProgress(object sender, ProgressChangedEventArgs e)
        {
            this.SetFileCounts(++this.numFilesDownloaded, this.numTotalDownloads);
            this.SetProgress((int)Math.Ceiling((double)this.numFilesDownloaded / (double)this.numTotalDownloads * 100.0));
        }

        private void Md5BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<AssetDownloader.AssetDescriptor> list = e.Result as List<AssetDownloader.AssetDescriptor>;
            if (list.Count == 0)
            {
                //Since there are 2 manifests, check for both
                if (!updateLauncher)
                    //Asset list
                    this.OnUpdatingFinished();
                else
                    //Launcher list
                    updateLauncher = false;
            }
            else
            {
                this.numFilesDownloaded = 0;
                this.numTotalDownloads = list.Count;
                this.SetFileCounts(0, this.numTotalDownloads);
                foreach (AssetDownloader.AssetDescriptor asset in list)
                    this.DownloadAsset(asset);
            }
        }

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
                return "skip";
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message);
                return "skip";
            }
        }

        private void OnAssetFileListDownloadProgressChanged(int totalPercentage)
        {
        }

        private void OnAssetFileListDownloadCompleted(List<AssetDownloader.AssetDescriptor> assetList)
        {
            this.GameDirectory = Environment.CurrentDirectory;
            this.settings.Save();
            this.WriteRegistryKeys();
            this.UpdateAssets(assetList);
        }

        private void OnAssetDownloadBegin(AssetDownloader.AssetDescriptor asset)
        {
            this.SetFilename(asset.Name);
        }

        private void OnAssetDownloadProgressChanged(int totalPercentage)
        {
            this.SetProgress(totalPercentage);
        }

        private void OnAssetDownloadCompleted(Asset asset)
        {
            using (FileStream fileStream = System.IO.File.Create(Path.Combine(this.GameDirectory, asset.FileName)))
            {
                using (MemoryStream memoryStream = new MemoryStream(asset.Data))
                {
                    using (GZipStream gzipStream = new GZipStream((Stream)memoryStream, CompressionMode.Decompress))
                        gzipStream.CopyTo((Stream)fileStream);
                }
            }
            this.downloadList.Remove(asset.Descriptor);
            ++this.numFilesDownloaded;
            this.SetFileCounts(this.numFilesDownloaded, this.numTotalDownloads);
            if (this.numFilesDownloaded != this.numTotalDownloads)
                return;

            this.OnUpdatingFinished();
        }
        #endregion

        #region Misc Private Calls
        private AccountServer.PingRequestStatusCode PingServer(string url)
        {
            switch (AccountServer.PingAccount(url))
            {
                default:
                case AccountServer.PingRequestStatusCode.NotFound:
                    return AccountServer.PingRequestStatusCode.NotFound;
                case AccountServer.PingRequestStatusCode.Ok:
                    return AccountServer.PingRequestStatusCode.Ok;
            }
        }

        private void WriteRegistryKeys()
        {
            for (int index = 0; index <= 5; ++index)
            {
                RegistryKey subKey = Registry.CurrentUser.CreateSubKey(string.Format("Software\\HarmlessGames\\Infantry\\Profile{0}\\Options", (object)index));
                subKey.SetValue("SDirectoryAddress", (object)this.settings["Launcher"]["Directory1"]);
                subKey.SetValue("SDirectoryAddressBackup", (object)this.settings["Launcher"]["Directory2"]);
            }
        }
        #endregion
    }
}
