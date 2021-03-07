using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace InfServer.Logic
{
    /// <summary>
    /// Allows for filtering of swear words.
    /// </summary>
    public static class SwearFilter
    {
        public static List<string> ForbiddenList = new List<string>();

        static SwearFilter()
        {
            LoadDictionary();
        }

        public static void LoadDictionary()
        {
            if (System.IO.File.Exists("../BIN/swear.txt"))
            {
                ForbiddenList = System.IO.File.ReadAllLines("../BIN/swear.txt").Select(t => t.Trim().ToLower()).ToList();
            }
        }

        /// <summary>
        /// Filters out a string of any potentially matched swear words and returns the filtered string.
        /// </summary>
        /// <param name="input">The input string</param>
        /// <param name="detected">Returns true if swear-like words are detected.</param>
        /// <returns></returns>
        public static string Filter(string input, out bool detected)
        {
            detected = false;

            var sb = new StringBuilder();

            // Add a trailing space so that we always have one last word to evaluate.
            input += ' ';

            char prev = ' ';
            int wordBeginIdx = -1;
            int len = input.Length;

            var breaks = new List<char> { ' ', '.', '!', ',', '?' };

            for (int i = 0; i < len; i++)
            {
                if (breaks.Contains(input[i]))
                {
                    if (wordBeginIdx != -1)
                    {
                        var w = input.Substring(wordBeginIdx, i - wordBeginIdx);
                        var wl = w.ToLower();

                        if (ForbiddenList.Contains(wl))
                        {
                            detected = true;
                            // sb.Append(new string('*', w.Length));
                        }
                        else
                        {
                            sb.Append(w);
                        }
                    }

                    sb.Append(input[i]);
                    wordBeginIdx = -1;
                }
                else if (breaks.Contains(prev))
                {
                    wordBeginIdx = i;
                }

                prev = input[i];
            }

            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();

            //var words = input.Split(new[] { ' ' }, StringSplitOptions.None);
            //var output = string.Empty;

            //foreach (var word in words)
            //{
            //    if (word.Length != 0)
            //    {
            //        var w = word;

            //        foreach (var f in ForbiddenList)
            //        {
            //            w = Regex.Replace(w, f, new string('*', f.Length), RegexOptions.IgnoreCase);
            //        }

            //        if (w != word)
            //        {
            //            detected = true;
            //        }

            //        output += w;
            //    }
            //    else
            //    {
            //        output += word;
            //    }

            //    // Space.
            //    output += ' ';
            //}

            //return output;
        }
    }
}
