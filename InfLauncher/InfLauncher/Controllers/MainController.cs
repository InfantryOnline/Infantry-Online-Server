using System;
using System.Windows.Forms;
using InfLauncher.Helpers;
using InfLauncher.Views;

namespace InfLauncher.Controllers
{
    /// <summary>
    /// The MainController handles all connection and inter-view events.
    /// </summary>
    public class MainController
    {
        /// <summary>
        /// The connection associated with this controller.
        /// </summary>
        private RestConnection _connection;

        /// <summary>
        /// The session id of an account (if any).
        /// </summary>
        private string _sessionId;

        /// <summary>
        /// 
        /// </summary>
        public MainForm Form { get; set; }

        /// <summary>
        /// Creates a new MainController and displays the initial form.
        /// </summary>
        public MainController()
        {
            _connection = new RestConnection();

            _connection.OnRegisterAccountResponse += OnAccountRegistrationResponse;
            _connection.OnLoginAccountResponse += OnAccountLoginResponse;
        }


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
            var form = new NewAccountForm(this);
            form.ShowDialog();
        }

        /// <summary>
        /// Requests a new account registration from the account server.
        /// </summary>
        /// <param name="username">The username to register</param>
        /// <param name="password">The password to register</param>
        public void RegisterAccount(string username, string password)
        {
            _connection.BeginRegisterAccount(username, password);
        }

        /// <summary>
        /// Requests to login with the account server.
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        public void LoginAccount(string username, string password)
        {
            _connection.BeginLoginAccount(username, password);
        }

        /// <summary>
        /// Returns the Session ID of the player if they are logged in; returns null otherwise.
        /// </summary>
        /// <returns></returns>
        public string GetSessionId()
        {
            return _sessionId;
        }

        #endregion


        #region Server Response Handlers

        /// <summary>
        /// Handles a response from the account server for a pending registration.
        /// </summary>
        /// <param name="successful">true if accoutn registration is successful</param>
        public void OnAccountRegistrationResponse(bool successful)
        {
            if(successful)
            {
                MessageBox.Show("Account successfully registered");
            }
            else
            {
                MessageBox.Show("Your account could not be registered.");
            }
        }

        /// <summary>
        /// Handles a response from the account server for a pending login.
        /// </summary>
        /// <param name="sessionId">Session Id for this player if successfully logged in; null otherwise</param>
        public void OnAccountLoginResponse(string sessionId)
        {
            if(sessionId == null)
            {
                MessageBox.Show("Could not login.");
            }
            else
            {
                MessageBox.Show("Logged in!");
                _sessionId = sessionId;
                Form.SetPlayButtonState(true);
            }
        }

        #endregion
    }
}
