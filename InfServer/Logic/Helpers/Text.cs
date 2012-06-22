using System.Text.RegularExpressions;

namespace InfServer.Logic
{
    public partial class Logic_Text
    {
        /// <summary>
        /// Removes any illegal non-infantry characters from a string
        /// </summary>
        public static string RemoveIllegalCharacters(string str)
        {   //Remove non-Infantry characters... trim whitespaces, and remove duplicate spaces
            string sb = "";
            foreach (char c in str)
                if (c >= ' ' && c <= '~')
                    sb += c;
            //Get rid of duplicate spaces
            Regex regex = new Regex(@"[ ]{2,}", RegexOptions.None);
            sb = regex.Replace(sb, @" ");
            //Trim it
            sb = sb.Trim();
            //We have our new Infantry compatible string!
            return sb;
        }
    }
}
