using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using InfLauncher.Helpers;
using InfLauncher.Models;
using InfLauncher.Protocol;
using InfLauncher.Views;

namespace InfLauncher.Controllers
{
    /// <summary>
    /// The MainController handles all connection and inter-view events.
    /// </summary>
    public class MainController : ApplicationContext
    {
        /// <summary>
        /// Controller that updates assets as needed.
        /// </summary>
        private AssetDownloadController assetController;

        /// <summary>
        /// Controller that handles downloading and displaying the latest news.
        /// </summary>
        private NewsController newsController;

        /// <summary>
        /// The connection associated with this controller.
        /// </summary>
        private RestConnection _connection;

        /// <summary>
        /// The session id of an account (if any).
        /// </summary>
        private string _sessionId;

        /// <summary>
        /// The string name of our account
        /// </summary>
        public string _account;

        /// <summary>
        /// 
        /// </summary>
        private NewAccountForm newAccountForm;

        /// <summary>
        /// 
        /// </summary>
        private MainForm mainForm;


        /// <summary>
        /// Creates a new MainController and displays the initial form.
        /// </summary>
        public MainController()
        {
            // Configure connection with the account server
            _connection = new RestConnection();

            _connection.OnRegisterAccountResponse += OnAccountRegistrationResponse;
            _connection.OnLoginAccountResponse += OnAccountLoginResponse;

            // Configure connection with the asset server
            assetController = new AssetDownloadController(Config.GetConfig().AssetsUrl);
            assetController.OnUpdatingFinished += OnUpdatingFinished;

            // Start for updates, go from there
            assetController.RunAsync();
        }

        #region Updater Delegate Handlers

        private void OnUpdatingFinished()
        {
            newAccountForm = new NewAccountForm(this);

            mainForm = new MainForm(this);
            mainForm.Closing += OnFormClosing;
            mainForm.Show();

            // Get the news!
            newsController = new NewsController("http://infdir1.aaerox.com/news/news.xml");

            newsController.OnNewsControllerDownloadProgressChanged += OnNewsControllerDownloadProgressChanged;
            newsController.OnNewsControllerDownloadCompleted += OnNewsControllerDownloadCompleted;
        }

        #endregion


        #region News Delegate Handlers

        private void OnNewsControllerDownloadProgressChanged(int totalPercentage)
        {
            // Not used.
        }

        private void OnNewsControllerDownloadCompleted(List<News> newsList)
        {
            mainForm.SetNews(newsList[0]);
        }

        #endregion

        #region Form Creation and Event Handlers

        /// <summary>
        /// Creates a new MainForm, showing a login box and other goodies.
        /// </summary>
        public void CreateMainForm()
        {
            var form = new MainForm(this);
            form.Show();
        }

        /// <summary>
        /// Creates a NewAccountForm, letting the user register a new account.
        /// </summary>
        public void CreateNewAccountForm()
        {
            newAccountForm.ShowDialog();
        }

        /// <summary>
        /// Requests a new account registration from the account server.
        /// </summary>
        /// <param name="username">The username to register</param>
        /// <param name="password">The password to register</param>
        public void RegisterAccount(Account.AccountRegistrationRequestModel requestModel)
        {
            if(_connection.BeginRegisterAccount(requestModel) != BeginRequestStatusCode.Ok)
            {
                MessageBox.Show("There was an error processing your request; please try again later.");
            }
        }

        /// <summary>
        /// Requests to login with the account server.
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        public void LoginAccount(Account.AccountLoginRequestModel requestModel)
        {
            if (_connection.BeginLoginAccount(requestModel) != BeginRequestStatusCode.Ok)
            {
                MessageBox.Show("There was an error processing your request; please try again later.");
            }
        }

        /// <summary>
        /// Returns the Session ID of the player if they are logged in; returns null otherwise.
        /// </summary>
        /// <returns></returns>
        public string GetSessionId()
        {
            return _sessionId;
        }

        private void OnFormClosing(object sender, CancelEventArgs e)
        {
            if(sender is MainForm)
            {
                ExitThread();
            }
        }

        #endregion


        #region Server Response Handlers

        /// <summary>
        /// Handles a response from the account server for a pending registration.
        /// </summary>
        /// <param name="response">Details of the registration response</param>
        public void OnAccountRegistrationResponse(RegistrationResponse response)
        {
            switch(response.Status)
            {
                case RegistrationStatusCode.Ok:
                    newAccountForm.Close();
                    MessageBox.Show("Your account has been successfully registered.");
                    break;

                case RegistrationStatusCode.UsernameTaken:
                    MessageBox.Show("The requested username is already taken. Please try again");
                    break;

                case RegistrationStatusCode.WeakCredentials:
                    MessageBox.Show("Double-check your username and email address.");
                    break;

                case RegistrationStatusCode.ServerError:
                    MessageBox.Show("Your request could not be processed. A server-side error has occured.");
                    break;

                case RegistrationStatusCode.MalformedData:
                    MessageBox.Show("An internal client error has occured. Your request could not be processed.");
                    break;
            }
        }

        /// <summary>
        /// Handles a response from the account server for a pending login.
        /// </summary>
        /// <param name="response">Details of the login response</param>
        public void OnAccountLoginResponse(LoginResponse response)
        {
            switch(response.Status)
            {
                case LoginStatusCode.Ok:
                    _sessionId = response.Model.TicketId.ToString();
                    mainForm.launchGame();
                    break;

                case LoginStatusCode.InvalidCredentials:
                    MessageBox.Show("Invalid username or password. Please try again.");
                    break;

                case LoginStatusCode.ServerError:
                    MessageBox.Show("Your request could not be processed. A server-side error has occured.");
                    break;

                case LoginStatusCode.MalformedData:
                    MessageBox.Show("An internal client error has occured. Your request could not be processed.");
                    break;
            }
        }

        #endregion
    }
}
