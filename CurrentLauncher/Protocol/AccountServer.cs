using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace InfantryLauncher.Protocol
{
    public class AccountServer
    {
        private static string reason = null;
        /// <summary>
        /// Returns a reason if the server sent one
        /// </summary>
        public static string Reason
        {
            get
            {
                return reason;
            }
        }

        /// <summary>
        /// Is the username valid?
        /// </summary>
        public static bool IsValidUsername(string user)
        {
            return user.Length >= 4;
        }

        /// <summary>
        /// Is the email valid?
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            return email.Contains("@");
        }

        /// <summary>
        /// Is the password valid?
        /// </summary>
        public static bool IsValidPassword(string pass)
        {
            return !string.IsNullOrWhiteSpace(pass);
        }

        /// <summary>
        /// Sends a ping request to our account server
        /// </summary>
        public static AccountServer.PingRequestStatusCode PingAccount(string pingUrl)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(pingUrl);
            httpWebRequest.Method = "GET";
            httpWebRequest.ContentType = "application/json";
            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    streamReader.ReadToEnd();
                    return AccountServer.PingRequestStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.NotFound:
                    default:
                        return AccountServer.PingRequestStatusCode.NotFound;
                }
            }
        }

        /// <summary>
        /// Sends a register request to our account server
        /// </summary>
        public static AccountServer.RegistrationStatusCode RegisterAccount(AccountServer.RegisterRequestObject requestModel, string RegisterUrl)
        {
            if (requestModel == null)
                throw new ArgumentNullException("requestModel");
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject((object)requestModel));
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(RegisterUrl);
            httpWebRequest.Method = "PUT";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.ContentLength = (long)bytes.Length;
            try
            {
                ((WebRequest)httpWebRequest).GetRequestStream().Write(bytes, 0, bytes.Length);
            }
            catch (WebException)
            {
                return AccountServer.RegistrationStatusCode.ServerError;
            }
            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    streamReader.ReadToEnd();
                    return AccountServer.RegistrationStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.Forbidden:
                        return AccountServer.RegistrationStatusCode.UsernameTaken;
                    case HttpStatusCode.Conflict:
                        return AccountServer.RegistrationStatusCode.EmailTaken;
                    case HttpStatusCode.NotAcceptable:
                        reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return AccountServer.RegistrationStatusCode.WeakCredentials;
                    case HttpStatusCode.InternalServerError:
                        reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return AccountServer.RegistrationStatusCode.ServerError;
                    case HttpStatusCode.Created:
                        return AccountServer.RegistrationStatusCode.Ok;
                    case HttpStatusCode.BadRequest:
                        return AccountServer.RegistrationStatusCode.MalformedData;
                    default:
                        return AccountServer.RegistrationStatusCode.ServerError;
                }
            }
        }

        /// <summary>
        /// Sends a login request to our account server
        /// </summary>
        public static AccountServer.LoginStatusCode LoginAccount(AccountServer.LoginRequestObject requestModel, string RegisterUrl, out AccountServer.LoginResponseObject payload)
        {
            if (requestModel == null)
                throw new ArgumentNullException("requestModel");
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject((object)requestModel));
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(RegisterUrl);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.ContentLength = (long)bytes.Length;
            try
            {
                ((WebRequest)httpWebRequest).GetRequestStream().Write(bytes, 0, bytes.Length);
            }
            catch (WebException)
            {
                payload = (AccountServer.LoginResponseObject)null;
                return AccountServer.LoginStatusCode.ServerError;
            }
            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    string str = streamReader.ReadToEnd();
                    Console.WriteLine(((object)str).ToString());
                    payload = JsonConvert.DeserializeObject<AccountServer.LoginResponseObject>(str);
                    return AccountServer.LoginStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        payload = (AccountServer.LoginResponseObject)null;
                        string response = ((HttpWebResponse)ex.Response).StatusDescription;
                        if (response != null && response != "")
                            reason = response;
                        return AccountServer.LoginStatusCode.MalformedData;
                    case HttpStatusCode.NotFound:
                        payload = (AccountServer.LoginResponseObject)null;
                        return AccountServer.LoginStatusCode.InvalidCredentials;
                    default:
                        payload = (AccountServer.LoginResponseObject)null;
                        return AccountServer.LoginStatusCode.ServerError;
                }
            }
        }

        public class LoginRequestObject
        {
            public string Username;
            public string PasswordHash;
        }

        public class RegisterRequestObject
        {
            public string Username;
            public string PasswordHash;
            public string Email;
        }

        public class LoginResponseObject
        {
            public string Username;
            public string Email;
            public Guid TicketId;
            public DateTime DateCreated;
            public DateTime LastAccessed;
            public int Permission;
        }

        public enum PingRequestStatusCode
        {
            Ok,
            NotFound,
        }

        public enum RegistrationStatusCode
        {
            Ok,
            MalformedData,
            UsernameTaken,
            EmailTaken,
            WeakCredentials,
            ServerError,
        }

        public enum LoginStatusCode
        {
            Ok,
            MalformedData,
            InvalidCredentials,
            ServerError,
        }
    }
}
