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
            _listenerThread = new Thread(_ =>
                                   {
                                       httpListener.Start();

                                       while (httpListener.IsListening)
                                       {
                                           HandleRequest(httpListener.GetContext());
                                       }
                                   });

            _listenerThread.Start();
        }

        /// <summary>
        /// Stops our listener thread
        /// </summary>
        public void Stop()
        {
            if (_listenerThread.IsAlive)
            {
                httpListener.Close();
                _listenerThread.Abort();
            }
        }

        #region Private Implementation and Helpers

        /// <summary>
        /// Initializes our HTTP listener with the directory listing service.
        /// </summary>
        private void InitializeListener()
        {
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
                    //Asset fetcher is requesting an update
                    if (request.Url.LocalPath.Contains("assetRequest"))
                    {
                        try
                        {
                            string localPath = request.Url.LocalPath;
                            int index = localPath.LastIndexOf("/");
                            responseString = (directoryServer.GetRequestedFile(localPath.Substring(index + 1)));
                            if (responseString == null)
                            {
                                response.StatusCode = 404;
                                response.OutputStream.Close();
                                break;
                            }
                        }
                        catch(Exception e)
                        {
                            Log.write(TLog.Warning, e.ToString());
                            response.StatusCode = 404;
                            response.OutputStream.Close();
                            break;
                        }
                    }
                    else if (request.Url.LocalPath.Contains("notz"))
                        responseString = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(from zone in directoryServer.Zones where !zone.Title.Contains("I:TZ") select new { zone.Title, zone.PlayerCount }));
                    else
                        responseString = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(from zone in directoryServer.Zones select new { zone.Title, zone.PlayerCount }));
                    response.ContentLength64 = responseString.Length;
                    response.OutputStream.Write(responseString, 0, responseString.Length);
                    response.OutputStream.Close();
                    break;

                case "PUT":
                    //Is the zone server providing us data?
                    if (!request.HasEntityBody)
                    {
                        response.StatusCode = 400;
                        response.OutputStream.Close();
                    }

                    string stringData = new System.IO.StreamReader(request.InputStream).ReadToEnd();
                    if (string.IsNullOrWhiteSpace(stringData))
                    {
                        response.StatusCode = 400;
                        response.OutputStream.Close();
                    }

                    //Done!
                    response.StatusCode = 201;
                    response.OutputStream.Close();

                    //Update
                    directoryServer.UpdateAssetList(stringData);
                    break;
            }
        }

        private readonly HttpListener httpListener;
        private readonly DirectoryServer directoryServer;
        private Thread _listenerThread;

        #endregion
    }
}
