using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using InfLauncher.Models;

namespace InfLauncher.Protocol
{
    public class NewsDownloader
    {
        /// <summary>
        /// The connecting object used to get the xml news file.
        /// </summary>
        private WebClient webClient;

        /// <summary>
        /// Directory on the server where the news.xml file resides.
        /// </summary>
        private string baseDirectoryUrl;

        public NewsDownloader(string baseDirectoryUrl)
        {
            if (baseDirectoryUrl == null)
            {
                throw new ArgumentNullException("baseDirectoryUrl");
            }

            this.baseDirectoryUrl = baseDirectoryUrl;
        }

        public void DownloadNewsFileAsync(string newsFileName)
        {
            if (newsFileName == null)
            {
                throw new ArgumentNullException("newsFileName");
            }

            webClient = new WebClient();

            // Hook in the async delegates
            webClient.DownloadProgressChanged += NewsListProgressChanged;
            webClient.DownloadDataCompleted += NewsListDownloadCompleted;

            webClient.DownloadDataAsync(new Uri(newsFileName));
        }

        #region Delegate Methods

        public delegate void NewsFileProgressChanged(int totalPercentageComplete);

        public delegate void NewsFileDownloadCompleted(List<News> newsList);

        public NewsFileProgressChanged OnNewsFileDownloadProgressChanged;

        public NewsFileDownloadCompleted OnNewsFileDownloadCompleted;

        #endregion

        #region WebClient Delegate Method Handlers

        private void NewsListProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnNewsFileDownloadProgressChanged(e.ProgressPercentage);
        }

        private void NewsListDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var list = new List<News>();
            var result = Encoding.UTF8.GetString(e.Result);
            
            // Parse the XML result into our News list.
            var xmlDoc = XDocument.Parse(result);

            foreach(var xmlPost in xmlDoc.Descendants("news"))
            {
                var post = new News(xmlPost.Element("title").Value,
                                    xmlPost.Element("url").Value,
                                    xmlPost.Element("description").Value);

                list.Add(post);
            }

            // TODO: Add error handling codes.

            OnNewsFileDownloadCompleted(list);
        }

        #endregion
    }
}
