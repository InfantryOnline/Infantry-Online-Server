using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using InfServer.StatServer.Objects;
using System.Collections.Generic;

namespace InfServer.StatServer
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
        public HttpJsonResponder(StatServer statServer)
        {
            if (statServer == null)
            {
                throw new ArgumentNullException("statServer");
            }

            this.statServer = statServer;

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
            httpListener.Prefixes.Add("http://*:80/stats/");
        }

        /// <summary>
        /// Handles the request for a zone listing.
        /// </summary>
        /// <param name="context">Listening context</param>
        private void HandleRequest(HttpListenerContext context)
        {

            Console.WriteLine("WUT");
            var request = context.Request;
            var response = context.Response;

            switch (request.HttpMethod)
            {
                case "GET":
                    string[] elements = request.Url.LocalPath.Split('/');


                    if (request.Url.LocalPath.Contains("squads"))
                    {
                        List<Squad> squads = statServer.getSquads(11);

                        var query = from row in squads.AsEnumerable()
                                    select new
                                    {
                                        id = (int)row.id,
                                        name = (string)row.name,
                                        rating = (int)row.stats.rating,
                                        wins = (int)row.stats.wins,
                                        losses = (int)row.stats.losses,
                                        kills = (int)row.stats.kills,
                                        deaths = (int)row.stats.deaths,
                                        points = (int)row.stats.points
                                    };

                        byte[] responseString = { };
                        responseString = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(query));
                        response.ContentLength64 = responseString.Length;
                        response.OutputStream.Write(responseString, 0, responseString.Length);
                        response.OutputStream.Close();
                    }
                    else
                    {
                        byte[] responseString = { };
                        responseString = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("No Stats"));
                        response.ContentLength64 = responseString.Length;
                        response.OutputStream.Write(responseString, 0, responseString.Length);
                        response.OutputStream.Close();
                    }
                    break;
            }
        }
   
        private readonly HttpListener httpListener;

        private readonly StatServer statServer;

        #endregion
    }
}
