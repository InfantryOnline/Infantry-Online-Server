using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using Infantry_Launcher.Helpers;
using Infantry_Launcher.Controllers;

namespace Infantry_Launcher
{
    public partial class MainForm : Form
    {
        #region Public Accessible Calls

        /// <summary>
        /// Updates our status message and displays the text
        /// </summary>
        public void UpdateStatusMsg(string status)
        {
            Status.Visible = true;
            Status.Text = status;
        }

        /// <summary>
        /// Disables our status messages
        /// </summary>
        public void DisableStatusMsg()
        {
            Status.Visible = false;
            Status.Text = string.Empty;
        }

        /// <summary>
        /// Updates our progress bar
        /// </summary>
        public void UpdateProgressBar(int totalPercentage)
        {
            ProgressBar.Value = totalPercentage;
        }

        /// <summary>
        /// When set by our patcher.exe, it will bypass downloading due to some error within the patcher
        /// </summary>
        public bool BypassDownload { get; set; }

        #endregion

        #region Setup and Initializes

        public MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            Shown += MainForm_Shown;
            AccountController.ShowReminder += ShowReminder;
            currentDirectory = Directory.GetCurrentDirectory(); //Lets set our working directory
        }

        /// <summary>
        /// Initiates our settings
        /// </summary>
        /// <returns>Returns true if successful</returns>
        public bool Initiate()
        {
            string settingsIni = (Path.Combine(currentDirectory, "settings.ini"));
            string defaultIni = (Path.Combine(currentDirectory, "default.ini"));
            if (File.Exists(settingsIni))
            {   //Do a quick ini comparison to see if there hasn't been an ini update
                IniCompare();

                settings = new IniFile(settingsIni);
                if (!settings.Load())
                { return false; }

                settings["Launcher"]["Version"] = Application.ProductVersion;
                Register.WriteAddressKeys(settings.Get("Launcher","Directory1"), settings.Get("Launcher","Directory2"));
                settings.Save();
                return true;
            }

            if (File.Exists(defaultIni))
            {
                try
                { File.Copy("default.ini", "settings.ini", false); }
                catch(Exception e)
                { MessageBox.Show(e.ToString()); }

                settings = new IniFile(settingsIni);
                if (!settings.Load())
                { return false; }

                settings["Launcher"]["Version"] = Application.ProductVersion;
                Register.WriteAddressKeys(settings.Get("Launcher", "Directory1"), settings.Get("Launcher", "Directory2"));
                settings.Save();
                return true;
            }

            MessageBoxForm msgBox = new MessageBoxForm();
            msgBox.Write("Error while Initiating Launcher",
                "Could not find your settings files.\r\nPlease make sure you installed the Infantry Launcher\r\nfrom our website below.");
            msgBox.ShowLinkLabel(GetWebsite());
            msgBox.ShowDialog();

            return false;
        }

        /// <summary>
        /// Load is called during initializing but before the layout is started
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            //Check for possible custom skinning and load it
            string imgs = (Path.Combine(currentDirectory, "imgs"));
            if (Directory.Exists(imgs))
            {
                try
                {
                    BackgroundImage = Image.FromFile(Path.Combine(imgs, "bg.png"), true);
                    imgBtnOff = Image.FromFile(Path.Combine(imgs, "btnoff.png"), true);
                    imgBtnOn = Image.FromFile(Path.Combine(imgs, "btnon.png"), true);
                    PlayButton.Image = imgBtnOff;
                    SignUpButton.Image = imgBtnOff;
                }
                catch //If they still dont exist just continue
                { }
            }

            LoadUserSettings();
            AcceptButton = PlayButton;
        }

        /// <summary>
        /// Called once the main form is shown to the user for the first time
        /// </summary>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            isDownloading = true;
            Refresh();

            PingServer();

            if (!ServerInactive)
            { CheckForUpdates(); }
        }

        #endregion

        #region Button Events

        private void WebsiteLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(GetWebsite());
        }

        private void DonateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(GetDonate());
        }

        private void DiscordLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(GetDiscord());
        }

        private void PswdHint_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //When clicked, allows you to change your password reminder
            string reminder = settings.Get("Credentials","Reminder");
            using (var form = new ReminderForm(reminder))
            {
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    settings["Credentials"]["Reminder"] = form.Reminder;
                    settings.Save();
                }
            }
        }

        private void ForgotPswd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //Server inactive? Don't do anything
            if (ServerInactive)
            { return; }

            if (MissingFile("Newtonsoft.Json.dll"))
            { return; }

            new RecoveryForm().ShowDialog();
        }

        private void SignUpButton_Click(object sender, EventArgs e)
        {
            //If the server is inactive, dont do anything
            if (ServerInactive)
            { return; }

            if (MissingFile("Newtonsoft.Json.dll"))
            { return; }

            if (string.IsNullOrEmpty(UsernameBox.Text) && string.IsNullOrEmpty(PasswordBox.Text))
            { new RegisterForm(settings).ShowDialog(); }
            else
            {   //Lets autofill first
                RegisterForm regForm = new RegisterForm(settings);
                regForm.AutoFill(UsernameBox.Text, PasswordBox.Text);
                regForm.ShowDialog();
            }
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            //If the server is inactive, dont do anything
            if (ServerInactive || isDownloading)
            { return; }

            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Username cannot be blank.", "Infantry Online");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Text))
            {
                MessageBox.Show("Password cannot be blank.", "Infantry Online");
                return;
            }

            //Try loading first
            string pswd = settings.Get("Credentials", "Password");

            //Did the text change at all?
            if (pswdTextChanged == true)
            { //Since it did, use this instead
                pswd = Md5.Hash(PasswordBox.Text.Trim());
            }

            switch (RememberPwd.CheckState)
            {
                case CheckState.Checked:
                    //Was the text changed? We don't overwrite till neccessary
                    if (pswdTextChanged == true)
                    { settings["Credentials"]["Password"] = Md5.Hash(PasswordBox.Text.Trim()); }
                    Register.WriteAddressKey("PasswordLength", "Launcher", PasswordBox.Text.Length.ToString());
                    break;

                case CheckState.Unchecked:
                    settings["Credentials"]["Password"] = string.Empty;
                    Register.DeleteValue("PasswordLength", "Launcher");
                    break;
            }

            settings["Credentials"]["Username"] = UsernameBox.Text.Trim();
            settings.Save();

            //Get our ticket id
            UpdateStatusMsg("Trying Credentials...");
            Cursor.Current = Cursors.WaitCursor;
            pswdTextChanged = false;

            if (MissingFile("Newtonsoft.Json.dll"))
            { return; }

            string[] response = AccountController.LoginServer(UsernameBox.Text.Trim(), pswd);
            if (response != null)
            {
                if (MissingFile("infantry.exe"))
                { return; }

                //Success, launch the game
                new Process()
                {
                    StartInfo =
                    {
                        FileName = Path.Combine(Environment.CurrentDirectory, "infantry.exe"),
                        Arguments = string.Format("/ticket:{0} /name:{1}", response[0], response[1])
                    }
                }.Start();

                Application.Exit();
            }

            Cursor.Current = Cursors.Default;
            DisableStatusMsg();
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            if (imgBtnOn != null)
            {
                ((Button)sender).Image = imgBtnOn;
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            if (imgBtnOff != null)
            {
                ((Button)sender).Image = imgBtnOff;
            }
        }

        #endregion

        #region Private Status Functions

        private void PasswordBox_TextChanged(object sender, EventArgs e)
        {
            pswdTextChanged = true;
        }

        private void LoadUserSettings()
        {
            string user = settings.Get("Credentials","Username");
            string pswd = settings.Get("Credentials","Password");

            UsernameBox.Select(); //Activate it

            if (user.Length <= 0)
            { return; }
            UsernameBox.Text = user;

            if (pswd.Length <= 0)
            { return; }
            string data = Register.GetKeyData("PasswordLength", "Launcher");
            string test = string.Empty;
            int length;
            if (string.IsNullOrEmpty(data) || !int.TryParse(data, out length))
            { return; }

            for (int c = 0; c < length; c++)
            { test += "*"; }
            PasswordBox.Text = test;
            pswdTextChanged = false;

            RememberPwd.Checked = true;
        }

        /// <summary>
        /// Pings the server asking if its running
        /// </summary>
        private void PingServer()
        {
            UpdateStatusMsg("Checking Server Status...");

            string url = settings.Get("Launcher","Accounts");
            if (!AccountController.PingServer(url))
            {
                url = settings.Get("Launcher","AccountsBackup");

                //First time failed, try backup
                if (!AccountController.PingServer(url))
                {
                    DisableStatusMsg();

                    MessageBox.Show("Server is currently not active.\r\nPlease try again in a few minutes or try reporting it to the admins using the discord link above.");
                    serverInactive = true;
                    if (!ServerLabel.Visible)
                    { ServerLabel.Visible = true; }
                    return;
                }
            }

            //Lets set our active server link
            AccountController.CurrentUrl = url;
            UpdateStatusMsg("Server is active.");
        }

        /// <summary>
        /// Checks for launcher and file updates
        /// </summary>
        private void CheckForUpdates()
        {
            //Check to see if we are bypassing
            if (BypassDownload)
            { //We are, bypass downloading
                UpdateComplete();
                return;
            }

            UpdateStatusMsg("Checking for launcher updates...");
            AssetDownloadController.CurrentDirectory = currentDirectory;
            AssetDownloadController.SetForm(this);
            try
            {
                string version = settings.Get("Launcher","Version");
                string versionUrl = new System.Net.WebClient().DownloadString(settings.Get("Launcher","VersionUrl"));
                if (!string.IsNullOrWhiteSpace(versionUrl.Trim()) && !version.Equals(versionUrl.Trim(), StringComparison.Ordinal))
                { //Strings dont match, check for greater than by parsing into int
                    if (VersionCheck(version, versionUrl))
                    { UpdateStatusMsg("No launcher files to update. Skipping..."); }
                    else
                    {
                        AssetDownloadController.OnUpdateLauncher += UpdateLauncher;
                        AssetDownloadController.DownloadAssets(settings.Get("Launcher", "LauncherAssets"), settings.Get("Launcher", "LauncherAssetsList"));
                        return;
                    }
                }
                else { UpdateStatusMsg("No launcher files to update. Skipping..."); }
            }
            catch
            { UpdateStatusMsg("Cannot download launcher updates....Skipping."); }

            UpdateFiles();
        }

        /// <summary>
        /// Activates our self updater and/or file downloads after launcher downloads are complete
        /// </summary>
        private void UpdateLauncher(bool skipUpdates)
        {
            if (skipUpdates)
            {
                UpdateStatusMsg("No files to update. Skipping...");

                UpdateFiles();
                return;
            }

            UpdateStatusMsg("Downloads complete, updating launcher...");
            System.Threading.Thread.Sleep(1000);

            if (File.Exists(Path.Combine(currentDirectory, "Patcher.exe")))
            {
                //See if we can even find the link to update
                string Location = settings.Get("Launcher", "LauncherAssets");
                string List = settings.Get("Launcher", "LauncherAssetsList");
                if (!string.IsNullOrEmpty(Location) && !string.IsNullOrEmpty(List))
                {
                    Process updater = new Process();
                    updater.StartInfo.FileName = "Patcher.exe";
                    updater.StartInfo.Arguments = string.Format("Location:{0} Manifest:{1}", Location, List);
                    updater.Start();

                    Application.Exit();
                }
            }

            UpdateStatusMsg("Cannot self update...Skipping.");

            UpdateFiles();
        }

        /// <summary>
        /// Updates our asset files
        /// </summary>
        private void UpdateFiles()
        {
            UpdateStatusMsg("Checking for file updates...");
            try
            {
                AssetDownloadController.OnUpdateFiles += UpdateComplete;
                AssetDownloadController.DownloadAssets(settings.Get("Launcher", "Assets"), settings.Get("Launcher", "AssetsList"));
            }
            catch
            {
                UpdateStatusMsg("Cannot download file updates....Skipping.");
                UpdateComplete();
            }
        }

        /// <summary>
        /// Activates our play button once all downloads are completed
        /// </summary>
        private void UpdateComplete()
        {
            UpdateStatusMsg("Updating complete...");
            PlayButton.Enabled = true;
            isDownloading = false;

            if (ProgressBar.Value != 100)
            { UpdateProgressBar(100); }

            //Delete any unnecessary files
            if (File.Exists(Path.Combine(currentDirectory, "Patcher.exe")))
            {
                //Set file attributes to normal incase of read-only then delete it
                File.SetAttributes(Path.Combine(currentDirectory, "Patcher.exe"), FileAttributes.Normal);
                File.Delete(Path.Combine(currentDirectory, "Patcher.exe"));
            }
        }

        #endregion

        #region Private Calls

        /// <summary>
        /// Checks our versions between strings by turning it into an int then checking for equals or greater than
        /// </summary>
        /// <returns>Returns true if it matches</returns>
        private bool VersionCheck(string currentVersion, string version)
        {
            try
            {
                int versionNumberA = int.Parse(StripChar(currentVersion.Trim(), '.', ' '));
                int versionNumberB = int.Parse(StripChar(version.Trim(), '.', ' '));
                if (versionNumberA >= versionNumberB)
                { return true; }
            }
            catch
            { }
            return false;
        }

        /// <summary>
        /// Parses through each INI file checking for matches and if needed, fixes them
        /// </summary>
        private void IniCompare()
        {
            string settingsIni = (Path.Combine(currentDirectory, "settings.ini"));
            string defaultIni = (Path.Combine(currentDirectory, "default.ini"));

            if (File.Exists(defaultIni))
            {
                Dictionary<string, string> settingsArray = new Dictionary<string, string>();
                Dictionary<string, string> defaultArray = new Dictionary<string, string>();
                string user = null, pass = null, remind = null;

                //First, lets load our default.ini and grab our data
                settings = new IniFile(defaultIni);
                if (settings.Load())
                {
                    foreach (string str in settings.GetElements())
                    {
                        foreach (KeyValuePair<string, string> sect in settings[str])
                        { defaultArray.Add(sect.Key, sect.Value); }
                    }
                }

                //Secondly, lets load our settings.ini to try matching it
                settings = new IniFile(settingsIni);
                if (settings.Load())
                {   //Lets save their credentials incase we need to overwrite the file
                    if (settings.ContainsKey("Credentials"))
                    {
                        if (settings["Credentials"].ContainsKey("Username"))
                        { user = settings["Credentials"]["Username"]; }

                        if (settings["Credentials"].ContainsKey("Password"))
                        { pass = settings["Credentials"]["Password"]; }

                        if (settings["Credentials"].ContainsKey("Reminder"))
                        { remind = settings["Credentials"]["Reminder"]; }
                    }

                    foreach (string str in settings.GetElements())
                    {
                        foreach (KeyValuePair<string, string> sect in settings[str])
                        { settingsArray.Add(sect.Key, sect.Value); }
                    }
                }
                //Now lets see if they match
                if (settingsArray.Count > 0 && defaultArray.Count > 0)
                {
                    bool overwrite = false;
                    foreach (KeyValuePair<string, string> kvp in defaultArray)
                    {
                        if (!settingsArray.ContainsKey(kvp.Key))
                        { overwrite = true; }

                        if (settingsArray.ContainsKey(kvp.Key) && !settingsArray[kvp.Key].Equals(kvp.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            //We dont want their login info
                            if (kvp.Key.Equals("Username") || kvp.Key.Equals("Password") || kvp.Key.Equals("Reminder"))
                            { continue; }

                            //If this is a version, check it
                            if (kvp.Key.Equals("Version") && VersionCheck(settingsArray[kvp.Key], kvp.Value))
                            { continue; }
                            overwrite = true;
                        }
                    }

                    //Are we missing Keys?
                    if (overwrite)
                    {
                        try
                        {
                            //Copy over it
                            File.SetAttributes(Path.Combine(currentDirectory, "settings.ini"), FileAttributes.Normal);
                            File.Copy("default.ini", "settings.ini", true);
                        }
                        catch (Exception e)
                        { MessageBox.Show(e.ToString()); }

                        //Lets reload, save their info then be on our way
                        settings = new IniFile(settingsIni);
                        if (settings.Load())
                        {
                            if (!string.IsNullOrWhiteSpace(user))
                            { settings["Credentials"]["Username"] = user; }

                            if (!string.IsNullOrWhiteSpace(pass))
                            { settings["Credentials"]["Password"] = pass; }

                            if (!string.IsNullOrWhiteSpace(remind))
                            { settings["Credentials"]["Reminder"] = remind; }
                            settings.Save();
                        }
                    }
                }
            }

            return;
        }

        private string GetWebsite()
        {
            string defaultLink = @"http://freeinfantry.com/";
            if (settings == null)
            { return defaultLink; }
            string website = settings.Get("Launcher", "Website");
            return !string.IsNullOrWhiteSpace(website) ? website : defaultLink;
        }

        private string GetDonate()
        {
            string defaultLink = @"http://freeinfantry.com/";
            if (settings == null)
            { return defaultLink; }
            string donate = settings.Get("Launcher", "DonateLink");
            return !string.IsNullOrWhiteSpace(donate) ? donate : defaultLink;
        }

        private string GetDiscord()
        {
            string defaultLink = @"https://discord.gg/2avPSyv";
            if (settings == null)
            { return defaultLink; }
            string discord = settings.Get("Launcher", "DiscordLink");
            return !string.IsNullOrWhiteSpace(discord) ? discord : defaultLink;
        }

        /// <summary>
        /// Shows the password reminder after a certain amount of invalid passwords
        /// </summary>
        private void ShowReminder(int count)
        {
            PswdHint.Enabled = true;
            PswdHint.Visible = true;

            //Get our reminder
            string reminder = settings.Get("Credentials", "Reminder");
            if (!string.IsNullOrWhiteSpace(reminder))
            {
                PswdHint.LinkBehavior = LinkBehavior.NeverUnderline;

                //Lets align our button
                int strLen = PswdHint.Text.Length + reminder.Length;
                PswdHint.Width = strLen;
                PswdHint.Location = new Point((Size.Width / 2) - (PswdHint.Width / 4 - PswdHint.Margin.Left), PswdHint.Location.Y);
                PswdHint.Text += reminder;
            }
        }

        /// <summary>
        /// If the server returned no signal, activate our message
        /// </summary>
        private bool ServerInactive
        {
            get
            {
                if (serverInactive)
                {
                    if (!ServerLabel.Visible)
                    { ServerLabel.Visible = true; }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Strips any character parameter out of a specified string
        /// </summary>
        private string StripChar(string str, params char[] remove)
        {
            string result = string.Empty;
            bool skip;
            foreach(char s in str)
            {
                skip = false;
                foreach(char c in remove)
                {
                    if (s == c)
                    { skip = true; }
                }
                if (!skip)
                { result += s; }
            }

            return result;
        }

        /// <summary>
        /// Determines if there is an important file missing in our directory and reports it
        /// </summary>
        private bool MissingFile(string fileName)
        {
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, fileName)))
            { return false; }

            Cursor.Current = Cursors.Default;

            MessageBox.Show(string.Format("Error: Cannot locate {0}.{1}Try restarting the launcher or using\n\rthe repair button in the installer.", fileName, "\n\r\n\r"), "File Missing");
            DisableStatusMsg();
            return true;
        }

        #endregion

        private IniFile settings;
        private string currentDirectory;
        private Image imgBtnOff;
        private Image imgBtnOn;
        private bool serverInactive;
        private bool isDownloading;
        private bool pswdTextChanged;
    }
}