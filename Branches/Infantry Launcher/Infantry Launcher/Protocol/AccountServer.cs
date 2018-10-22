using Newtonsoft.Json;

using System;
using System.IO;
using System.Net;
using System.Text;

using Infantry_Launcher.Protocol.Helpers;

namespace Infantry_Launcher.Protocol
{
    public class AccountServer
    {
        /// <summary>
        /// Returns a reason if the server sent us one
        /// </summary>
        public static string Reason { get; private set; }

        /// <summary>
        /// Sends a ping request to our account server
        /// </summary>
        public static IStatus.PingRequestStatusCode PingAccount(string pingUrl)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(pingUrl);
            httpWebRequest.Method = "GET";
            httpWebRequest.ContentType = "application/json";
            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    streamReader.ReadToEnd();
                    return IStatus.PingRequestStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                if ((HttpWebResponse)ex.Response == null)
                { return IStatus.PingRequestStatusCode.NotFound; }

                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.NotFound:
                    default:
                        return IStatus.PingRequestStatusCode.NotFound;
                }
            }
        }

        /// <summary>
        /// Sends a register request to our account server
        /// </summary>
        public static IStatus.RegistrationStatusCode RegisterAccount(IStatus.RegisterRequestObject requestModel, string RegisterUrl)
        {
            if (requestModel == null)
            { throw new ArgumentNullException("requestModel"); }

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestModel));
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(RegisterUrl);
            httpWebRequest.Method = "PUT";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.ContentLength = bytes.Length;
            try
            {
                httpWebRequest.GetRequestStream().Write(bytes, 0, bytes.Length);
            }
            catch (WebException)
            {
                return IStatus.RegistrationStatusCode.ServerError;
            }

            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    streamReader.ReadToEnd();
                    return IStatus.RegistrationStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                if ((HttpWebResponse)ex.Response == null)
                { return IStatus.RegistrationStatusCode.NoResponse; }

                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.Forbidden:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RegistrationStatusCode.UsernameTaken;
                    case HttpStatusCode.Conflict:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RegistrationStatusCode.EmailTaken;
                    case HttpStatusCode.NotAcceptable:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RegistrationStatusCode.WeakCredentials;
                    case HttpStatusCode.InternalServerError:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RegistrationStatusCode.ServerError;
                    case HttpStatusCode.Created:
                        return IStatus.RegistrationStatusCode.Ok;
                    case HttpStatusCode.BadRequest:
                        return IStatus.RegistrationStatusCode.MalformedData;

                    default:
                        return IStatus.RegistrationStatusCode.ServerError;
                }
            }
        }

        /// <summary>
        /// Sends a login request to our account server
        /// </summary>
        public static IStatus.LoginStatusCode LoginAccount(IStatus.LoginRequestObject requestModel, string LoginUrl, out IStatus.LoginResponseObject payload)
        {
            if (requestModel == null)
            { throw new ArgumentNullException("requestModel"); }

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestModel));
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(LoginUrl);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.ContentLength = bytes.Length;
            try
            {
                httpWebRequest.GetRequestStream().Write(bytes, 0, bytes.Length);
            }
            catch (WebException)
            {
                payload = null;
                return IStatus.LoginStatusCode.ServerError;
            }

            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    string str = streamReader.ReadToEnd();
                    payload = JsonConvert.DeserializeObject<IStatus.LoginResponseObject>(str);
                    if (string.IsNullOrWhiteSpace(payload.TicketId.ToString()) || string.IsNullOrWhiteSpace(payload.Username))
                    {
                        payload = null;
                        Reason = "Incorrect username or password.";
                        return IStatus.LoginStatusCode.MalformedData;
                    }
                    return IStatus.LoginStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                if ((HttpWebResponse)ex.Response == null)
                {
                    payload = null;
                    return IStatus.LoginStatusCode.NoResponse;
                }

                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        payload = null;
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.LoginStatusCode.MalformedData;
                    case HttpStatusCode.NotFound:
                        payload = null;
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.LoginStatusCode.InvalidCredentials;

                    default:
                        payload = null;
                        return IStatus.LoginStatusCode.ServerError;
                }
            }
        }

        /// <summary>
        /// Sends a recovery request to our account server
        /// </summary>
        public static IStatus.RecoverStatusCode RecoverAccount(IStatus.RecoverRequestObject requestModel, string RequestUrl, out string payload)
        {
            payload = null;
            if (requestModel == null)
            { throw new ArgumentNullException("requestModel"); }

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestModel));
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(RequestUrl);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.ContentLength = bytes.Length;
            try
            {
                httpWebRequest.GetRequestStream().Write(bytes, 0, bytes.Length);
            }
            catch (WebException)
            { return IStatus.RecoverStatusCode.ServerError; }

            try
            {
                using (StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
                {
                    payload = streamReader.ReadToEnd();
                    return IStatus.RecoverStatusCode.Ok;
                }
            }
            catch (WebException ex)
            {
                if ((HttpWebResponse)ex.Response == null)
                { return IStatus.RecoverStatusCode.NoResponse; }

                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RecoverStatusCode.MalformedData;
                    case HttpStatusCode.NotFound:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RecoverStatusCode.InvalidCredentials;
                    case HttpStatusCode.InternalServerError:
                        Reason = ((HttpWebResponse)ex.Response).StatusDescription;
                        return IStatus.RecoverStatusCode.ServerError;

                    default:
                        return IStatus.RecoverStatusCode.ServerError;
                }
            }
        }

    }
}