using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using InfLauncher.Helpers;
using InfLauncher.Models;
using Newtonsoft.Json;

namespace InfLauncher.Protocol
{
    #region Application-level Status Codes

    /// <summary>
    /// A status code that is returned when the request is first sent off to the server.
    /// </summary>
    public enum BeginRequestStatusCode
    {
        /// <summary>
        /// The request is successfully sent.
        /// </summary>
        Ok,

        /// <summary>
        /// An error sending the request has occured.
        /// </summary>
        Error
    }

    /// <summary>
    /// Status codes for account registration requests.
    /// </summary>
    public enum RegistrationStatusCode
    {
        /// <summary>
        /// The account is successfully registered.
        /// </summary>
        Ok,

        /// <summary>
        /// The registration data sent is malformed.
        /// </summary>
        MalformedData,

        /// <summary>
        /// The requested username is already taken.
        /// </summary>
        UsernameTaken,

        /// <summary>
        /// The username or password combination is unsatisfactory.
        /// </summary>
        WeakCredentials,

        /// <summary>
        /// An unknown server error occured.
        /// </summary>
        ServerError,
    }

    /// <summary>
    /// Status codes for account login requests.
    /// </summary>
    public enum LoginStatusCode
    {
        /// <summary>
        /// The account is successfully logged in.
        /// </summary>
        Ok,

        /// <summary>
        /// The login data sent is malformed.
        /// </summary>
        MalformedData,

        /// <summary>
        /// The username or password are invalid.
        /// </summary>
        InvalidCredentials,

        /// <summary>
        /// An unknown server error occured.
        /// </summary>
        ServerError,
    }

    #endregion

    #region Application-level Response objects

    public class RegistrationResponse
    {
        internal RegistrationResponse(RegistrationStatusCode status)
        {
            Status = status;
        }

        public RegistrationStatusCode Status { get; private set; }
    }

    public class LoginResponse
    {
        internal LoginResponse(LoginStatusCode status, Account.AccountLoginResponseModel model)
        {
            Status = status;
            Model = model;
        }

        public LoginStatusCode Status { get; private set; }
        public Account.AccountLoginResponseModel Model { get; private set; }
    }

    #endregion

    /// <summary>
    /// Provides an interface to a RESTful protocol with the account server.
    /// </summary>
    public class RestConnection
    {
        /// <summary>
        /// The base URL address (including the port) of the account server.
        /// </summary>
        public static string BaseDomain = Config.GetConfig().AccountsUrl;

        /// <summary>
        /// Relative path of the URL to create accounts.
        /// </summary>
        public static string RegisterUrl = String.Format("{0}/Account", BaseDomain);

        /// <summary>
        /// Relative path of the URL to login.
        /// </summary>
        public static string LoginUrl = String.Format("{0}/Account", BaseDomain);


        #region Response Delegates

        /// <summary>
        /// Called when a response has been received for a pending account registration.
        /// </summary>
        /// <param name="succeeded">True if account is successfully registered</param>
        public delegate void ReceivedRegisterAccountResponseDelegate(RegistrationResponse response);

        /// <summary>
        /// Called when a response has been received for a pending login.
        /// </summary>
        /// <param name="guid">If successful, the session id of this account; null if failed</param>
        public delegate void ReceivedLoginAccountResponseDelegate(LoginResponse response);

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
        /// <param name="requestModel">Data model for the registration request</param>
        /// <returns>true if successfully started; false otherwise</returns>
        public BeginRequestStatusCode BeginRegisterAccount(Account.AccountRegistrationRequestModel requestModel)
        {
            if (requestModel == null)
            {
                throw new ArgumentNullException("requestModel");
            }

            // Create the registration form
            requestModel.PasswordHash = CalculateHashFor(requestModel.PasswordHash);
            string jsonModel = JsonConvert.SerializeObject(requestModel);

            byte[] contentBody = Encoding.UTF8.GetBytes(jsonModel);

            // Create the request
            var request = (HttpWebRequest) WebRequest.Create(RegisterUrl);
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.ContentLength = contentBody.Length;

            try
            {
                var stream = request.GetRequestStream();
                stream.Write(contentBody, 0, contentBody.Length);
            }
            catch (WebException)
            {
                return BeginRequestStatusCode.Error;
            }

            // Off it goes!
            try
            {
                request.BeginGetResponse(AsyncRegisterAccountResponse, request);
            }
            catch (Exception)
            {
                return BeginRequestStatusCode.Error;
            }

            return BeginRequestStatusCode.Ok;
        }

        /// <summary>
        /// Starts the login procedure to the account server.
        /// 
        /// When the response (if any) is received, the listeners associated with OnLoginAccountResponse will be notified.
        /// </summary>
        /// <param name="requestModel">Data model for the login request</param>
        /// <returns>true if successfully started; false otherwise</returns>
        public BeginRequestStatusCode BeginLoginAccount(Account.AccountLoginRequestModel requestModel)
        {
            if(requestModel == null)
            {
                throw new ArgumentNullException("requestModel");
            }

            // Create the login form
            requestModel.PasswordHash = CalculateHashFor(requestModel.PasswordHash);
            string jsonModel = JsonConvert.SerializeObject(requestModel);

            byte[] contentBody = Encoding.UTF8.GetBytes(jsonModel);

            // Create the request
            var request = (HttpWebRequest) WebRequest.Create(LoginUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = contentBody.Length;

            try
            {
                var stream = request.GetRequestStream();
                stream.Write(contentBody, 0, contentBody.Length);
            }
            catch (WebException)
            {
                return BeginRequestStatusCode.Error;
            }

            // Off it goes!
            try
            {
                request.BeginGetResponse(AsyncLoginAccountResponse, request);
            }
            catch (Exception)
            {
                return BeginRequestStatusCode.Error;
            }

            return BeginRequestStatusCode.Ok;
        }

        #endregion


        #region HttpWebRequest Async Response Methods

        /// <summary>
        /// Called when a response is received for a pending account registration.
        /// </summary>
        /// <param name="result">Response data</param>
        private void AsyncRegisterAccountResponse(IAsyncResult result)
        {
            RegistrationStatusCode status = RegistrationStatusCode.ServerError;

            var request = result.AsyncState as HttpWebRequest;
            try
            {
                var response = (HttpWebResponse)request.EndGetResponse(result);

                if (HttpStatusCode.Created == response.StatusCode)
                {
                    status = RegistrationStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                using (HttpWebResponse response = (HttpWebResponse)ex.Response)
                {
                    switch(response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            status = RegistrationStatusCode.MalformedData;
                            break;

                        case HttpStatusCode.Forbidden:
                            status = RegistrationStatusCode.UsernameTaken;
                            break;

                        case HttpStatusCode.NotAcceptable:
                            status = RegistrationStatusCode.WeakCredentials;
                            break;

                        case HttpStatusCode.InternalServerError:
                            status = RegistrationStatusCode.ServerError;
                            break;
                    }
                }
            }

            OnRegisterAccountResponse(new RegistrationResponse(status));
        }

        /// <summary>
        /// Called when a response is received for a pending account login.
        /// </summary>
        /// <param name="result">Response data</param>
        private void AsyncLoginAccountResponse(IAsyncResult result)
        {
            LoginStatusCode status = LoginStatusCode.ServerError;
            Account.AccountLoginResponseModel responseModel = null;

            var request = result.AsyncState as HttpWebRequest;

            try
            {
                var response = (HttpWebResponse)request.EndGetResponse(result);

                if(HttpStatusCode.OK == response.StatusCode)
                {
                    // Grab the account data.
                    var reader = new StreamReader(response.GetResponseStream());

                    responseModel = JsonConvert.DeserializeObject<Account.AccountLoginResponseModel>(reader.ReadToEnd());

                    status = LoginStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                using(HttpWebResponse response = (HttpWebResponse)ex.Response)
                {
                    switch(response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            status = LoginStatusCode.MalformedData;
                            break;

                        case HttpStatusCode.NotFound:
                            status = LoginStatusCode.InvalidCredentials;
                            break;

                        case HttpStatusCode.InternalServerError:
                            status = LoginStatusCode.ServerError;
                            break;
                    }
                }
            }

            OnLoginAccountResponse(new LoginResponse(status, responseModel));
        }

        #endregion


        #region Private Helper Methods

        /// <summary>
        /// Returns the SHA1 calculated hash for a given string |str|.
        /// </summary>
        /// <param name="str">The input string</param>
        /// <returns>SHA1 hashed output string</returns>
        private static string CalculateHashFor(string str)
        {
            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(str);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();

            for (var i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("x2"));

            return sb.ToString();
        }

        #endregion
    }
}
