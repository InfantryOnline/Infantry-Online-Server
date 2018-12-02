using System;
using System.Collections.Generic;
using InfLauncher.Models;
using InfLauncher.Protocol;

namespace InfLauncher.Controllers
{
    public class NewsController
    {
        private readonly NewsDownloader newsDownloader;

        public NewsController(string baseUrlDirectory)
        {
            if(baseUrlDirectory == null)
            {
                throw new ArgumentNullException("baseUrlDirectory");
            }

            newsDownloader = new NewsDownloader("");

            newsDownloader.OnNewsFileDownloadProgressChanged += OnNewsFileDownloadProgressChanged;
            newsDownloader.OnNewsFileDownloadCompleted += OnNewsFileDownloadCompleted;

            newsDownloader.DownloadNewsFileAsync("http://freeinfantry.org/news/news.xml");
        }

        private void OnNewsFileDownloadProgressChanged(int totalPercentage)
        {
            OnNewsControllerDownloadProgressChanged(totalPercentage);
        }

        private void OnNewsFileDownloadCompleted(List<News> newsList)
        {
            OnNewsControllerDownloadCompleted(newsList);
        }

        #region Delegate Methods

        public delegate void NewsControllerFileProgressChanged(int totalPercentageComplete);

        public delegate void NewsControllerDownloadCompleted(List<News> newsList);

        public NewsControllerFileProgressChanged OnNewsControllerDownloadProgressChanged;

        public NewsControllerDownloadCompleted OnNewsControllerDownloadCompleted;

        #endregion
    }
}
