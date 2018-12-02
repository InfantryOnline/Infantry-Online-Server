using System.Windows.Forms;

using Infantry_Launcher.Protocol;
using Infantry_Launcher.Protocol.Helpers;

namespace Infantry_Launcher.Controllers
{
    public static class AccountController
    {
        public static event PasswordReminder ShowReminder;
        public delegate void PasswordReminder(int count);

        /// <summary>
        /// Gets or sets our current active url in string form
        /// </summary>
        public static string CurrentUrl
        {
            get; set;
        }

        /// <summary>
        /// Pings the server, checking for activity
        /// </summary>
        /// <remarks>Sets the ping type to status(0)</remarks>
        public static bool PingServer(string Url)
        {
            if (string.IsNullOrEmpty(Url))
            { return false; }

            IStatus.PingRequestStatusCode ping = AccountServer.PingAccount(Url);
            return (ping == IStatus.PingRequestStatusCode.Ok ? true : false);
        }

        /// <summary>
        /// Sends a login request to the server
        /// </summary>
        public static string[] LoginServer(string Username, string Password)
        {
            string url = CurrentUrl;
            IStatus.LoginResponseObject payload;
            IStatus.LoginRequestObject request = new IStatus.LoginRequestObject();
            request.Username = Username;
            request.PasswordHash = Password;

            string msg;
            switch(AccountServer.LoginAccount(request, CurrentUrl, out payload))
            {
                case IStatus.LoginStatusCode.Ok:
                    string[] getPayload = { payload.TicketId.ToString(), payload.Username };
                    return getPayload;

                case IStatus.LoginStatusCode.InvalidCredentials:
                    msg = "Error: Invalid username/password";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Login Request");
                    if (passwordCount++ >= 3)
                    {
                        if (ShowReminder != null)
                        { ShowReminder(passwordCount); }
                    }
                    break;

                case IStatus.LoginStatusCode.MalformedData:
                    msg = "Error: malformed username/password";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Login Request");
                    if (passwordCount++ >= 3)
                    {
                        if (ShowReminder != null)
                        { ShowReminder(passwordCount); }
                    }
                    break;

                case IStatus.LoginStatusCode.ServerError:
                    msg = "Server error, could not connect.\r\n Is your firewall enabled?";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : AccountServer.Reason, "Login Request");
                    break;

                case IStatus.LoginStatusCode.NoResponse:
                    msg = "Server error, received no response from the server.";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : AccountServer.Reason, "Login Request");
                    break;
            }

            return null;
        }

        /// <summary>
        /// Sends a recovery request to the server
        /// </summary>
        public static bool RecoveryRequest(string Username, string Email, bool Reset)
        {
            string url = CurrentUrl + @"/recover";
            string payload;
            IStatus.RecoverRequestObject request = new IStatus.RecoverRequestObject();
            request.Username = Username;
            request.Email = Email;
            request.Reset = Reset;

            string msg;
            switch (AccountServer.RecoverAccount(request, url, out payload))
            {
                case IStatus.RecoverStatusCode.Ok:
                    MessageBox.Show("Recovery Successful!\n\rCheck your email at " + payload, "Recovery Request");
                    return true;

                case IStatus.RecoverStatusCode.InvalidCredentials:
                    msg = "Error: Invalid username/email";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Recovery Request");
                    break;

                case IStatus.RecoverStatusCode.MalformedData:
                    msg = "Error: malformed username/email";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Recovery Request");
                    break;

                case IStatus.RecoverStatusCode.ServerError:
                    msg = "Server error, could not connect.\r\n Is your firewall enabled?";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Recovery Request");
                    break;

                case IStatus.RecoverStatusCode.NoResponse:
                    msg = "Server error, received no response from the server.";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Recovery Request");
                    break;
            }

            return false;
        }

        /// <summary>
        /// Registers a new account
        /// </summary>
        public static bool RegisterAccount(string Username, string Password, string Email)
        {
            string msg;
            IStatus.RegisterRequestObject request = new IStatus.RegisterRequestObject();
            request.Username = Username;
            request.PasswordHash = Helpers.Md5.Hash(Password);
            request.Email = Email;

            switch(AccountServer.RegisterAccount(request, CurrentUrl))
            {
                case IStatus.RegistrationStatusCode.Ok:
                    MessageBox.Show("Account created!", "Register Request");
                    return true;

                case IStatus.RegistrationStatusCode.MalformedData:
                    msg = "Error: malformed username/password";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Register Request");
                    break;

                case IStatus.RegistrationStatusCode.UsernameTaken:
                    msg = "Error: Username is taken.";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Register Request");
                    break;

                case IStatus.RegistrationStatusCode.EmailTaken:
                    msg = "Error: Email is already used.";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Register Request");
                    break;

                case IStatus.RegistrationStatusCode.WeakCredentials:
                    msg = "too short/invalid characters/invalid email";
                    MessageBox.Show("Error: Credentials are invalid.\n\r(" 
                        + (string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : AccountServer.Reason) +")", "Register Request");
                    break;

                case IStatus.RegistrationStatusCode.ServerError:
                    msg = "Server error, could not connect. Is your firewall enabled?";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Register Request");
                    break;

                case IStatus.RegistrationStatusCode.NoResponse:
                    msg = "Server error, received no response from the server.";
                    MessageBox.Show(string.IsNullOrWhiteSpace(AccountServer.Reason) ? msg : "Error: " + AccountServer.Reason, "Register Request");
                    break;
            }

            return false;
        }

        private static int passwordCount = 0;
    }
}
