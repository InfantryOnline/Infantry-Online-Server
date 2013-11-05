using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace InfServer.DirectoryServer.Directory.Protocol
{
    /// <summary>
    /// Returns a list of game servers and their respective player counts.
    /// </summary>
    public class HttpJsonResponder
    {
        /// <summary>
        /// Creates the responder with a directory server.
        /// </summary>
        /// <param name="directoryServer">Directory server to use</param>
        public HttpJsonResponder(DirectoryServer directoryServer)
        {
            if(directoryServer == null)
            {
                throw new ArgumentNullException("directoryServer");
            }

            this.directoryServer = directoryServer;

            httpListener = new HttpListener();
            InitializeListener();
        }

        /// <summary>
        /// Begins to service requests from clients.
        /// </summary>
        public void Start()
        {
            var t = new Thread(_ =>
                                   {
                                       httpListener.Start();

                                       while (true)
                                       {
                                           HandleRequest(httpListener.GetContext());
                                       }
                                   });

            t.Start();
        }


        #region Private Implementation and Helpers

        /// <summary>
        /// Initializes our HTTP listener with the directory listing service.
        /// </summary>
        private void InitializeListener()
        {
            //prefixes.ToList().ForEach(p => httpListener.Prefixes.Add(p));
            httpListener.Prefixes.Add(directoryServer._jsonURI);
        }

        /// <summary>
        /// Handles the request for a zone listing.
        /// </summary>
        /// <param name="context">Listening context</param>
        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            switch(request.HttpMethod)
            {
                case "GET":
                    byte[] responseString;
                    if (request.Url.LocalPath.Contains("notz"))
                        responseString = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(from zone in directoryServer.Zones where !zone.Title.Contains("I:TZ") select new { zone.Title, zone.PlayerCount }));
                    else
                        responseString = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(from zone in directoryServer.Zones select new { zone.Title, zone.PlayerCount }));
                    response.ContentLength64 = responseString.Length;
                    response.OutputStream.Write(responseString, 0, responseString.Length);
                    response.OutputStream.Close();
                    break;
            }
        }

        private readonly HttpListener httpListener;

        private readonly DirectoryServer directoryServer;

        #endregion
    }
}
