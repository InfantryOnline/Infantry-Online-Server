using System.Text;

namespace InfServer.Logic
{
    public partial class Logic_Text
    {
        /// <summary>
        /// Removes any illegal non-infantry characters from a string
        /// </summary>
        public static string RemoveIllegalCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
            return sb.ToString();
        }
    }
}
