using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using Microsoft.VisualBasic;

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
            UpdateStatusMsg(status, false);
        }

        /// <summary>
        /// Updates our status message, displays the text, then refreshes the form for an immediate result
        /// </summary>
        public void UpdateStatusMsg(string status, bool refresh)
        {
            Status.Visible = true;
            Status.Text = status;

            if (refresh)
            { Refresh(); }
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
            string ddrawIniPath = (Path.Combine(currentDirectory, "ddraw.ini"));

            if (File.Exists(ddrawIniPath))
            {
                ddrawIni = new IniFile(ddrawIniPath);
                ddrawIni.Load();
            }

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

            SetDefaultRegistryKeys(false);

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

            if (string.IsNullOrWhiteSpace(UsernameBox.Text.Trim()))
            {
                MessageBox.Show("Username cannot be blank.", "FreeInfantry");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Text.Trim()))
            {
                MessageBox.Show("Password cannot be blank.", "FreeInfantry");
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
                    {
                        settings["Credentials"]["Password"] = Md5.Hash(PasswordBox.Text.Trim());
                        Register.WriteAddressKey("PasswordLength", "Launcher", (PasswordBox.Text.Trim()).Length.ToString());
                    }
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
                if (MissingFile("FreeInfantry.exe"))
                { return; }

                //Success, launch the game
                new Process()
                {
                    StartInfo =
                    {
                        FileName = Path.Combine(Environment.CurrentDirectory, "FreeInfantry.exe"),
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

        private void SetSingleHKCURegistryKey(string path, string valueName, object value, Boolean forceOverride = false)
        {
            RegistryKey currentUserRegistry = Registry.CurrentUser;

            // Try to open the Requested Path
            var reg = currentUserRegistry.OpenSubKey(path, true);
            // If the Path does NOT exist
            if (reg == null)
            {
                // Create the Path
                reg = currentUserRegistry.CreateSubKey(path);
            }

            // If the Property does not exist OR if there is a forceOverride requested
            if ((reg.GetValue(valueName) == null) || (forceOverride))
            {
                // Then set the value given (object takes any value, a number will be a DWORD and a string will be a STRING
                reg.SetValue(valueName, value);
            }

            reg.Close();
        }

        private void SetDefaultRegistryKeys(Boolean forceOverride = false, string whichSection = "All")
        {
            // WINE ddraw override (Steam Deck, Steam Proton, Linux, Mac) (useless/harmless on Windows)
            SetSingleHKCURegistryKey("Software\\Wine\\DllOverrides", "ddraw", "native,builtin", true);

            string basePath = "Software\\HarmlessGames\\Infantry\\";

            // MISC DEFAULTS
            SetSingleHKCURegistryKey(basePath + "Misc", "Accepted", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "BL", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "BP", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "GlobalNewsCrc", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "HighPriority", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "LastProfile", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "LastVersionExecuted", 156, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "ReleaseNotesCrc", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "SC", 0, true);
            SetSingleHKCURegistryKey(basePath + "Misc", "ST", 0, true);

            // PROFILE DEFAULTS
            for (int i = 0; i < 6; i++)
            {
                string baseProfilePath = basePath + "Profile" + i + "\\";

                if ((whichSection == "All") || (whichSection == "Login") || (whichSection == "Profile"+(i+1)))
                {
                    SetSingleHKCURegistryKey(baseProfilePath + "Login", "name", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Login", "ParentName", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Login", "ServerName", "", forceOverride);
                }

                if ((whichSection == "All") || (whichSection == "Channels") || (whichSection == "Profile" + (i + 1)))
                {
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel0", "newbies", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel1", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel2", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel3", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel4", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel5", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel6", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel7", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Chat", "Channel8", "", forceOverride);
                }

                if ((whichSection == "All") || (whichSection == "Zoom") || (whichSection == "Profile" + (i + 1)))
                {
                    SetSingleHKCURegistryKey(baseProfilePath + "HiddenOptions", "ZoomMax", 1000, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "HiddenOptions", "ZoomTime", 400, forceOverride);
                }

                if ((whichSection == "All") || (whichSection == "Controls") || (whichSection == "Profile" + (i + 1)))
                {
                    bool newControls = true;

                    if (newControls) {
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "0", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "1", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "2", 22806528, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "3", 21757952, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "4", 32505856, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "5", 32768000, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "6", 17039360, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "7", 17825792, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "8", 4238848, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "9", 4236800, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "10", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "11", 262144, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "12", 524288, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "13", 18087936, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "14", 21233664, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "15", 23592960, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "16", 23068672, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "17", 17563648, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "18", 22544384, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "19", 21495808, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "20", 12845056, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "21", 13107200, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "22", 13369344, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "23", 13631488, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "24", 18350080, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "25", 1048576, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "26", 17301504, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "27", 0, forceOverride); // 2359296
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "28", 23330816, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "29", 22020096, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "30", 57933824, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "31", 57409536, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "32", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "33", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "34", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "35", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "36", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "37", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "38", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "39", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "40", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "41", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "42", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "43", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "44", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "45", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "46", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "47", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "48", 0, forceOverride); // 29884416
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "49", 31195136, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "50", 30932992, forceOverride); // 19660800
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "51", 30670848, forceOverride); // 19922944
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "52", 22282240, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "53", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "54", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "55", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "56", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "57", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "58", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "59", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "60", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "61", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "62", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "63", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "64", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "65", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "66", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "67", 18874368, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "68", 18612224, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "69", 1310720, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisDeadzone", 8000, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisRotate", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisStrafe", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisThrust", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "EnterForMessages", 1, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "LeftRight", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MouseLeft", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MouseMiddle", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MouseRight", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MovementMode", 3, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "ReservedFirstLetters", "~ `+-", forceOverride);
                    } else {
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "0", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "1", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "2", 22806528, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "3", 21757952, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "4", 12845056, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "5", 13107200, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "6", 17039360, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "7", 17825792, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "8", 4238848, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "9", 4236800, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "10", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "11", 262144, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "12", 1048576, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "13", 524288, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "14", 21233664, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "15", 18087936, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "16", 4456448, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "17", 21495808, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "18", 18350080, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "19", 8388608, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "20", 18612224, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "21", 18874368, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "22", 9437184, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "23", 18874368, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "24", 22020096, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "25", 23330816, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "26", 22282240, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "27", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "28", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "29", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "30", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "31", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "32", 4718592, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "33", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "34", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "35", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "36", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "37", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "38", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "39", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "40", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "41", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "42", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "43", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "44", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "45", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "46", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "47", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "48", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "49", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "50", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "51", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "52", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "53", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "54", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "55", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "56", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "57", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "58", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "59", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "60", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "61", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "62", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "63", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "64", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "65", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "66", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "67", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "68", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "69", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisDeadzone", 8000, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisRotate", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisStrafe", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "AxisThrust", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "EnterForMessages", 1, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "LeftRight", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MouseLeft", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MouseMiddle", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MouseRight", 0, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "MovementMode", 3, forceOverride);
                        SetSingleHKCURegistryKey(baseProfilePath + "Keyboard", "ReservedFirstLetters", "~ `+-", forceOverride);
                    }
                }

                if ((whichSection == "All") || (whichSection == "Chat") || (whichSection == "Profile" + (i + 1)))
                {

                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "Alarm", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "Entering", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterChat", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterKill", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterPopup", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterPrivate", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterPublic", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterPublicMacro", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterSquad", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterSystem", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FilterTeam", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "FixCase", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "Height", 84, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "Leaving", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Message", "NameWidth", 84, forceOverride);
                }

                if ((whichSection == "All") || (whichSection == "Interface") || (whichSection == "Profile" + (i + 1)))
                {
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "AdjustSoundDelay", 5, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "AlternateClock", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "AutoLogMessages", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "AvoidPageFlipping", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "BannerCacheSize", 500, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "BlockObscene2", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ChatChannelEntering", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "CoordinateMode", 3, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "DeathMessageMode", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "DetailLevel", 2, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "DisableJoystick", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "DisplayLosAlpha", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "DisplayLosMode", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "DisplayMapGrid", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "EnergyPercent", 600, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "EnvironmentAudio", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "FakeAlpha", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "HideSmartTrans", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "IsAllowSpectators", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "MainRectBottom", 1199, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "MainRectLeft", 38, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "MainRectRight", 1052, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "MainRectTop", 632, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "NotepadLastWidth", 160, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "NotepadWidth", 160, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "PlayerListHeightPercent", 3506, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "PlayerSortMode", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RadarGammaPercent", 1000, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RenderBackground", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RenderParallax", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RenderStarfield", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RollMode", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RotateRampTime", 25, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RotationCount", 64, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "RotationSounds", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "SDirectoryAddress", "infdir1.aaerox.com", true); // FORCED
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "SDirectoryAddressBackup", "infdir2.aaerox.com", true); // FORCED
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowAimingTick", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowBallTrails", 500, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowBanners", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowClock", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowDifficultyLevel", 100, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowEnemyThrust", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowEnergyBar", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowFrameRate", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowHealthGuage", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowKeystrokes", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowLogo", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowMessageType", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowPhysics", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowSelfName", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowTerrain", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowTrails", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowVehiclePhysics", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ShowVision", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "SkipSplash", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "Sound3d", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "SoundVolume", 10, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "Squad", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "SquadPassword", "", forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ThrustSounds", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "TipOfDay", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "TipOfDayPosition", 12, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "TransparentMessageArea", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "TransparentNotepad", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "TripleBuffer", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemBackBuffer", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemVirtualBuffer", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemZoomBuffer", 0, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "VertSync", 1, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ViewPercentToEdge", 500, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ViewSpeed", 5, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ZoneSpecificMacros", 0, forceOverride);
                }

                if ((whichSection == "All") || (whichSection == "Resolution") || (whichSection == "Profile" + (i + 1)))
                {
                    // Set Next Values to Current Monitor Resolution
                    Screen myScreen = Screen.PrimaryScreen; //Screen.FromControl(UsernameBox);
                    Rectangle area = myScreen.Bounds;

                    int defaultWidth = area.Width;
                    int defaultHeight = area.Height;

                    /*
                    if (defaultWidth > 1920)
                    {
                        defaultWidth = 1920;
                    }
                    if (defaultHeight > 1080)
                    {
                        defaultHeight = 1080;
                    }
                    */

                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ResolutionX", defaultWidth, forceOverride);
                    SetSingleHKCURegistryKey(baseProfilePath + "Options", "ResolutionY", defaultHeight, forceOverride);
                }
            }
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
            {
                length = 8; // IF WE DON'T KNOW HOW LONG, JUST PUT IN 8 ASTERISKS.
                //return;
            }

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
            UpdateStatusMsg("Checking Server Status...", true);

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

            UpdateStatusMsg("Checking for server message...", true);

            try
            {
            string launcherAlertMessage = new System.Net.WebClient().DownloadString(settings.Get("Launcher", "LauncherAssets") + "alert.txt");

                if (!string.IsNullOrWhiteSpace(launcherAlertMessage.Trim()))
                {
                    MessageBox.Show(launcherAlertMessage, "Launcher Alert");
                }
            }
            catch
            { /* do nothing, okay to skip */ }


            // Creating a FAT installer/launcher without Auto Asset Downloading
            /*
            //Check to see if we are bypassing
            if (BypassDownload)
            { //We are, bypass downloading
            */
                UpdateComplete();
                return;
            /*
            }
            */

            // Removing Launcher "Auto-Update"
            /*
            UpdateStatusMsg("Checking for launcher updates...", true);
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

            */

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
            UpdateStatusMsg("Checking for file updates...", true);
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
            // Creating a FAT installer/launcher without Auto Asset Downloading
            UpdateStatusMsg("Game is up to date. Sign in and press Play!");
            //UpdateStatusMsg("Updating complete...");
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
        private IniFile ddrawIni;
        private string currentDirectory;
        private Image imgBtnOff;
        private Image imgBtnOn;
        private bool serverInactive;
        private bool isDownloading;
        private bool pswdTextChanged;
        private ContextMenu buttonMenu;
        private string currentZoomSpeed = "400";

        private void setupGearButtonMenu()
        {

            // TODO: clear memory if already initialized...

            string zoomTimeDataGet = Register.GetKeyData("ZoomTime", "Profile0\\HiddenOptions");

            // MessageBox.Show("zoomTimeDataGet = " + zoomTimeDataGet);

            if (!string.IsNullOrEmpty(zoomTimeDataGet)) {
                currentZoomSpeed = zoomTimeDataGet;
            }

            MenuItem advancedMenu = new MenuItem("Advanced");

            // TODO: check if ddrawIni loaded correctly!
            bool ddrawIniExists = (ddrawIni != null);

            if (ddrawIniExists) {
                string currentDDrawRenderer = ddrawIni.Get("ddraw", "renderer");
                //string currentResolutionsNumber = ddrawIni.Get("infantry", "resolutions"); // ("FreeInfantry", "resolutions")
                string currentMouseIsUnlocked = ddrawIni.Get("ddraw", "devmode");
                
                MenuItem ddrawMenu = new MenuItem("cnc-ddraw");
                MenuItem ddrawMenuRendererAuto = new MenuItem("Set renderer to auto (dx9/opengl)", gear_action_ddraw_change);
                ddrawMenuRendererAuto.Checked = (currentDDrawRenderer == "auto");
                ddrawMenu.MenuItems.Add(ddrawMenuRendererAuto);
                MenuItem ddrawMenuRendererDx9 = new MenuItem("Set renderer to dx9", gear_action_ddraw_change);
                ddrawMenuRendererDx9.Checked = (currentDDrawRenderer == "direct3d9");
                ddrawMenu.MenuItems.Add(ddrawMenuRendererDx9);
                MenuItem ddrawMenuRendererOpenGL = new MenuItem("Set renderer to opengl", gear_action_ddraw_change);
                ddrawMenuRendererOpenGL.Checked = (currentDDrawRenderer == "opengl");
                ddrawMenu.MenuItems.Add(ddrawMenuRendererOpenGL);
                ddrawMenu.MenuItems.Add(new MenuItem("-"));
                MenuItem ddrawMenuMouseSetLocked = new MenuItem("Lock Mouse to Game Window", gear_action_ddraw_change);
                ddrawMenuMouseSetLocked.Checked = (currentMouseIsUnlocked == "false");
                ddrawMenu.MenuItems.Add(ddrawMenuMouseSetLocked);
                MenuItem ddrawMenuMouseSetUnlocked = new MenuItem("Unlock Mouse from Game Window", gear_action_ddraw_change);
                ddrawMenuMouseSetUnlocked.Checked = (currentMouseIsUnlocked == "true");
                ddrawMenu.MenuItems.Add(ddrawMenuMouseSetUnlocked);
                /*
                ddrawMenu.MenuItems.Add(new MenuItem("-"));
                MenuItem ddrawMenuResolutionsFull = new MenuItem("Set resolutions to 2 (Full List)", gear_action_ddraw_change);
                ddrawMenuResolutionsFull.Checked = (currentResolutionsNumber == "2");
                ddrawMenu.MenuItems.Add(ddrawMenuResolutionsFull);
                MenuItem ddrawMenuResolutionsSmall = new MenuItem("Set resolutions to 0 (Small List)", gear_action_ddraw_change);
                ddrawMenuResolutionsSmall.Checked = (currentResolutionsNumber == "0");
                ddrawMenu.MenuItems.Add(ddrawMenuResolutionsSmall);
                */

                advancedMenu.MenuItems.Add(ddrawMenu);
            }


            // Zoom Speed 
            MenuItem zoomSpeedMenu = new MenuItem("Zoom Speed ("+currentZoomSpeed+")");
            MenuItem zoomSpeedSetTo400 = new MenuItem("Reset to default: 400", gear_action_zoomspeed_change);
            MenuItem zoomSpeedSetToOther = new MenuItem("Set to other...", gear_action_zoomspeed_change);

            if (currentZoomSpeed != "400") {
                zoomSpeedMenu.MenuItems.Add(zoomSpeedSetTo400);
            }
            zoomSpeedMenu.MenuItems.Add(zoomSpeedSetToOther);

            advancedMenu.MenuItems.Add(zoomSpeedMenu);

            // FPS Drawing
            MenuItem fpsLimitMenu = new MenuItem("FPS Drawing Limit");
            MenuItem fpfLimitMenuProfiles = new MenuItem("Settings Profile");
            MenuItem[] fpfLimitMenuProfilesEach = new MenuItem[6];
            for (int i = 0; i < fpfLimitMenuProfilesEach.Length; i++)
            {
                // TODO: check if set to 60 or unlimited currently...
                // TODO, check ddraw setting
                // TODO: check registry settings for limiting...

                fpfLimitMenuProfilesEach[i] = new MenuItem("Profile #" + (i + 1));

                MenuItem fpfLimitMenuProfilesEachSetTo60 = new MenuItem("Set Profile #" + (i + 1) + " FPS limit to: 60", gear_action_fpslimit_change);
                //fpfLimitMenuProfilesEachSetTo60.Checked = (currentFPSLimit == 60);
                fpfLimitMenuProfilesEach[i].MenuItems.Add(fpfLimitMenuProfilesEachSetTo60);

                MenuItem fpfLimitMenuProfilesEachSetToUnlimited = new MenuItem("Set Profile #" + (i + 1) + " FPS limit to: UNLIMITED", gear_action_fpslimit_change);
                //fpfLimitMenuProfilesEachSetTo60.Checked = (currentFPSLimit == 60);
                fpfLimitMenuProfilesEach[i].MenuItems.Add(fpfLimitMenuProfilesEachSetToUnlimited);

                fpfLimitMenuProfiles.MenuItems.Add(fpfLimitMenuProfilesEach[i]);
            }
            fpsLimitMenu.MenuItems.Add(fpfLimitMenuProfiles);
            MenuItem fpsLimitMenuSetTo60 = new MenuItem("Set all FPS to: 60", gear_action_fpslimit_change);
            //fpsLimitMenuSetTo60.Checked = (currentFPSLimit == 60);
            fpsLimitMenu.MenuItems.Add(fpsLimitMenuSetTo60);
            MenuItem fpsLimitMenuSetToUnlimited = new MenuItem("Set all FPS to: UNLIMITED", gear_action_fpslimit_change);
            //fpsLimitMenuSetToUnlimited.Checked = (currentFPSLimit == 0);
            fpsLimitMenu.MenuItems.Add(fpsLimitMenuSetToUnlimited);

            advancedMenu.MenuItems.Add(fpsLimitMenu);

            MenuItem resetMenu = new MenuItem("Reset/Clear");
            MenuItem specificSettingsProfile = new MenuItem("Settings Profile");
            for (int i = 0; i < 6; i++)
            {
                specificSettingsProfile.MenuItems.Add(new MenuItem("Reset Profile #" + (i + 1) + "...", gear_action_clear_settings));
            }
            resetMenu.MenuItems.Add(specificSettingsProfile);
            resetMenu.MenuItems.Add(new MenuItem("Saved Login...", gear_action_clear_settings));
            //resetMenu.MenuItems.Add(new MenuItem("All Key/Mouse Controls...", gear_action_clear_settings));
            resetMenu.MenuItems.Add(new MenuItem("All Registry Settings...", gear_action_clear_settings));

            MenuItem[] menuItems = new MenuItem[] { resetMenu, advancedMenu, new MenuItem("Close") };

            buttonMenu = new ContextMenu(menuItems);
        }


        private void GearButton_Click(object sender, EventArgs e)
        {
            setupGearButtonMenu();
            buttonMenu.Show(GearButton, new System.Drawing.Point(11, 11));
        }

        private void set_fpslimit_for_profile(int profileNumber, int fpslimit = 60)
        {
            if (ddrawIni != null)
            {
                ddrawIni["ddraw"]["maxgameticks"] = "0";
                ddrawIni.Save();
            }

            string basePath = "Software\\HarmlessGames\\Infantry\\";
            string baseProfilePath = basePath + "Profile" + (profileNumber-1) + "\\";

            if (fpslimit == -1) // -1 = unlimited...
            {
                SetSingleHKCURegistryKey(baseProfilePath + "Options", "AvoidPageFlipping", 1, true);
                SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemBackBuffer", 1, true);
                SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemVirtualBuffer", 1, true);
            }
            else
            {
                SetSingleHKCURegistryKey(baseProfilePath + "Options", "AvoidPageFlipping", 0, true);
                SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemBackBuffer", 0, true);
                SetSingleHKCURegistryKey(baseProfilePath + "Options", "UseSystemVirtualBuffer", 0, true);
            }
        }

        private void gear_action_zoomspeed_change(object sender, EventArgs e)
        {
            MenuItem sentMenuItem = sender as MenuItem;

            // TODO!
            if (sentMenuItem.Text == "Reset to default: 400")
            {
                for (int i = 0; i < 6; i++)
                {
                    string basePath = "Software\\HarmlessGames\\Infantry\\";
                    string baseProfilePath = basePath + "Profile" + i + "\\";
                    SetSingleHKCURegistryKey(baseProfilePath + "HiddenOptions", "ZoomTime", 400, true);
                }
            }
            else
            {

                string input = 
                    Interaction.InputBox("From 100 to 1000, default 400.", 
                                                            "Set ZoomSpeed", 
                                                            currentZoomSpeed,
                                                            -1, -1); // TODO: set to CURRENT SETTING
                if (input != "") {
                    int requestedZoomSpeed;
                    bool success = int.TryParse(input, out requestedZoomSpeed);

                    if (success)
                    {
                        if ((requestedZoomSpeed >= 100) && (requestedZoomSpeed <= 1000)) {
                            for (int i = 0; i < 6; i++)
                            {
                                string basePath = "Software\\HarmlessGames\\Infantry\\";
                                string baseProfilePath = basePath + "Profile" + i + "\\";
                                SetSingleHKCURegistryKey(baseProfilePath + "HiddenOptions", "ZoomTime", requestedZoomSpeed, true);
                            }
                        }
                    }
                }
            }
        }

        private void gear_action_fpslimit_change(object sender, EventArgs e)
        {
            MenuItem sentMenuItem = sender as MenuItem;
            if (sentMenuItem.Text == "Set all FPS to: 60")
            {
                for (int i = 0; i < 6; i++)
                {
                    set_fpslimit_for_profile((i + 1), 60);
                }
                MessageBox.Show("All Profiles '" + sentMenuItem.Text + "' is complete.");
            }
            else if (sentMenuItem.Text == "Set all FPS to: UNLIMITED")
            {
                for (int i = 0; i < 6; i++)
                {
                    set_fpslimit_for_profile((i + 1), -1); // -1 = unlimited...
                }
                MessageBox.Show("All Profiles '" + sentMenuItem.Text + "' is complete.");
            }
            else if (sentMenuItem.Text.Contains("Set Profile #"))
            {
                string profileNumberString = Regex.Match(sentMenuItem.Text, @"\d+").Value; // Will grab FIRST number it finds...
                if (sentMenuItem.Text.Contains("UNLIMITED")) {
                    set_fpslimit_for_profile(Int32.Parse(profileNumberString), -1); // -1 = unlimited...

                } else {
                    //string fpslimitNumberString = profileNumberString.NextMatch();
                    set_fpslimit_for_profile(Int32.Parse(profileNumberString), 60); // TODO: add more options than 60

                }
                MessageBox.Show("'"+sentMenuItem.Text + "' is complete.");
            }
            else
            {
                Console.WriteLine("Unknown fpfLimit MenuItem: " + sentMenuItem.Text);
                return;
            }
        }

        private void gear_action_ddraw_change(object sender, EventArgs e)
        {
            MenuItem sentMenuItem = sender as MenuItem;
            // MessageBox.Show(sentMenuItem.Text);
            if (sentMenuItem.Text == "Set renderer to auto (dx9/opengl)")
            {
                ddrawIni["ddraw"]["renderer"] = "auto";
                ddrawIni.Save();
            }
            else if (sentMenuItem.Text == "Set renderer to dx9")
            {
                ddrawIni["ddraw"]["renderer"] = "direct3d9";
                ddrawIni.Save();
            }
            else if (sentMenuItem.Text == "Set renderer to opengl")
            {
                ddrawIni["ddraw"]["renderer"] = "opengl";
                ddrawIni.Save();
            }
            else if (sentMenuItem.Text == "Lock Mouse to Game Window")
            {
                ddrawIni["ddraw"]["devmode"] = "false";
                ddrawIni.Save();
            }
            else if (sentMenuItem.Text == "Unlock Mouse from Game Window")
            {
                ddrawIni["ddraw"]["devmode"] = "true";
                ddrawIni.Save();
            }
            else if (sentMenuItem.Text == "Set resolutions to 2 (Full List)")
            {
                //ddrawIni["infantry"]["resolutions"] = "2"; // ddrawIni["FreeInfantry"]["resolutions"]
                //ddrawIni.Save();
            }
            else if (sentMenuItem.Text == "Set resolutions to 0 (Small List)")
            {
                //ddrawIni["infantry"]["resolutions"] = "0"; // ddrawIni["FreeInfantry"]["resolutions"]
                //ddrawIni.Save();
            }
            else
            {
                Console.WriteLine("Unknown ddraw MenuItem: "+sentMenuItem.Text);
                return;
            }

        }
        private void gear_action_clear_settings(object sender, EventArgs e)
        {

            MenuItem sentMenuItem = sender as MenuItem;

            string whatToReset= "";
            string alertMessage= "";
            string alertTitle= "Confirm Reset";

            if (sentMenuItem.Text == "Reset Profile #1...")
            {
                whatToReset= "Profile1";
                alertMessage= "Are you sure to reset \"Settings Profile #1\" controls & settings to defaults?";
            }
            else if (sentMenuItem.Text == "Reset Profile #2...")
            {
                whatToReset= "Profile2";
                alertMessage= "Are you sure to reset \"Settings Profile #2\" controls & settings to defaults?";
            }
            else if (sentMenuItem.Text == "Reset Profile #3...")
            {
                whatToReset= "Profile3";
                alertMessage= "Are you sure to reset \"Settings Profile #3\" controls & settings to defaults?";
            }
            else if (sentMenuItem.Text == "Reset Profile #4...")
            {
                whatToReset= "Profile4";
                alertMessage= "Are you sure to reset \"Settings Profile #4\" controls & settings to defaults?";
            }
            else if (sentMenuItem.Text == "Reset Profile #5...")
            {
                whatToReset= "Profile5";
                alertMessage= "Are you sure to reset \"Settings Profile #5\" controls & settings to defaults?";
            }
            else if (sentMenuItem.Text == "Reset Profile #6...")
            {
                whatToReset= "Profile6";
                alertMessage= "Are you sure to reset \"Settings Profile #6\" controls & settings to defaults?";
            }
            else if (sentMenuItem.Text == "Saved Login...")
            {
                whatToReset= "savedLogin";
                alertMessage= "Are you sure to clear the saved login?";
            }
            else if (sentMenuItem.Text == "All Registry Settings...")
            {
                whatToReset= "All";
                alertMessage= "Are you sure to reset all FreeInfantry controls & settings to defaults?";
            }
            else
            {
                Console.WriteLine("Unknown Reset MenuItem: "+sentMenuItem.Text);
                return;
            }

            if (whatToReset != "") {
                var confirmResult =  MessageBox.Show(alertMessage+"\r\n\r\nTHERE IS NO UNDO.",
                                        alertTitle,
                                        MessageBoxButtons.YesNo);
                if (confirmResult == DialogResult.Yes)
                {
                    if (whatToReset == "savedLogin")
                    {
                        settings["Credentials"]["Username"] = "";
                        settings["Credentials"]["Password"] = "";
                        settings["Credentials"]["Reminder"] = "";
                        settings.Save();
                        UsernameBox.Text = "";
                        PasswordBox.Text = "";
                        RememberPwd.Checked = false;
                        UsernameBox.Select();
                    }
                    else
                    {
                        SetDefaultRegistryKeys(true, whatToReset); // True for forcing...
                        MessageBox.Show("Reset '" + sentMenuItem.Text + "' is complete.");
                    }
                }
            }
        }
    }
}