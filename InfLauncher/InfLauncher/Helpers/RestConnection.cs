using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace InfLauncher.Helpers
{
    /// <summary>
    /// Provides an interface to a RESTful protocol with the account server.
    /// </summary>
    public class RestConnection
    {
        /// <summary>
        /// The base URL address (including the port) of the account server.
        /// </summary>
        public static string BaseDomain = "http://localhost:52940";

        /// <summary>
        /// Relative path of the URL to create accounts.
        /// </summary>
        public static string RegisterUrl = String.Format("{0}/Account/Create", BaseDomain);

        /// <summary>
        /// Relative path of the URL to login.
        /// </summary>
        public static string LoginUrl = String.Format("{0}/Account/Login", BaseDomain);

        #region Response Delegates

        /// <summary>
        /// Called when a response has been received for a pending account registration.
        /// </summary>
        /// <param name="succeeded">True if account is successfully registered</param>
        public delegate void ReceivedRegisterAccountResponseDelegate(bool succeeded);

        /// <summary>
        /// Called when a response has been received for a pending login.
        /// </summary>
        /// <param name="guid">If successful, the session id of this account; null if failed</param>
        public delegate void ReceivedLoginAccountResponseDelegate(string guid);

        /// <summary>
        /// Called when a response has been received for a pending account registration.
        /// </summary>
        public ReceivedRegisterAccountResponseDelegate OnRegisterAccountResponse;

        /// <summary>
        /// Called when a response has been received for a pending login.
        /// </summary>
        public ReceivedLoginAccountResponseDelegate OnLoginAccountResponse;

        #endregion


        #region Account Methods

        /// <summary>
        /// Starts the registration procedure with the account with the account server.
        /// 
        /// When the response (if any) is received, the listeners associated with OnRegisterAccountResponse will be notified.
        /// </summary>
        /// <param name="username">Account's username</param>
        /// <param name="password">Account's password</param>
        /// <returns>true if successfully started; false otherwise</returns>
        public bool BeginRegisterAccount(string username, string password)
        {
            if (username == null)
                throw new ArgumentNullException("username");
            if (password == null)
                throw new ArgumentNullException("password");

            // Create the registration form
            byte[] form = Encoding.UTF8.GetBytes(String.Format("username={0}&password={1}", username, password));

            // Create the request
            var request = (HttpWebRequest) WebRequest.Create(RegisterUrl);
            request.Method = "PUT";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = form.Length;
            var stream = request.GetRequestStream();
            stream.Write(form, 0, form.Length);

            // Off it goes!
            request.BeginGetResponse(AsyncRegisterAccountResponse, request);
            return true;
        }

        /// <summary>
        /// Starts the login procedure to the account server.
        /// 
        /// When the response (if any) is received, the listeners associated with OnLoginAccountResponse will be notified.
        /// </summary>
        /// <param name="username">Account's username</param>
        /// <param name="password">Account's password</param>
        /// <returns>true if successfully started; false otherwise</returns>
        public bool BeginLoginAccount(string username, string password)
        {
            if (username == null)
                throw new ArgumentNullException("username");
            if (password == null)
                throw new ArgumentNullException("password");

            // Create the login form
            byte[] form = Encoding.UTF8.GetBytes(String.Format("username={0}&password={1}", username, password));

            // Create the request
            var request = (HttpWebRequest) WebRequest.Create(LoginUrl);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = form.Length;
            var stream = request.GetRequestStream();
            stream.Write(form, 0, form.Length);

            // Off it goes!
            request.BeginGetResponse(AsyncLoginAccountResponse, request);
            return true;
        }

        #endregion


        #region HttpWebRequest Async Response Methods

        /// <summary>
        /// Called when a response is received for a pending account registration.
        /// </summary>
        /// <param name="result">Response data</param>
        private void AsyncRegisterAccountResponse(IAsyncResult result)
        {
            var request = result.AsyncState as HttpWebRequest;
            var response = (HttpWebResponse) request.EndGetResponse(result);
            var success = false;

            if (HttpStatusCode.Created == response.StatusCode)
            {
                success = true;
            }

            OnRegisterAccountResponse(success);
        }

        /// <summary>
        /// Called when a response is received for a pending account login.
        /// </summary>
        /// <param name="result">Response data</param>
        private void AsyncLoginAccountResponse(IAsyncResult result)
        {
            var request = result.AsyncState as HttpWebRequest;
            var response = (HttpWebResponse) request.EndGetResponse(result);

            if(HttpStatusCode.OK == response.StatusCode)
            {
                var reader = new StreamReader(response.GetResponseStream());
                var responseData = JsonConvert.DeserializeObject<LoginResponseData>(reader.ReadToEnd());

                OnLoginAccountResponse(responseData.SessionId.ToString());
            }
            else if (HttpStatusCode.ResetContent == response.StatusCode)
            {
                OnLoginAccountResponse(null);
            }
        }

        #endregion
    }
}
