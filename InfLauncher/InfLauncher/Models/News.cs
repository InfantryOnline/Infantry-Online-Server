using System;

namespace InfLauncher.Models
{
    public class News
    {
        /// <summary>
        /// The title of this news post.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// The URL Address of this news post.
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        /// The description of this news post.
        /// </summary>
        public string Description { get; private set; }


        public News(string newsTitle, string newsURL, string newsDescription)
        {
            if (newsTitle == null)
            {
                throw new ArgumentNullException("newsTitle");
            }
            if (newsURL == null)
            {
                throw new ArgumentNullException("newsURL");
            }
            if (newsDescription == null)
            {
                throw new ArgumentNullException("newsDescription");
            }

            Title = newsTitle;
            URL = newsURL;
            Description = newsDescription;
        }
    }
}
