using System.Security.Cryptography;
using System.Text;

namespace InfantryLauncher.Classes
{
    internal class Md5
    {
        /// <summary>
        /// Creates an md5 hash of a specific string
        /// </summary>
        public static string Hash(string str)
        {
            byte[] hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(str));
            StringBuilder stringBuilder = new StringBuilder();

            for (int index = 0; index < hash.Length; ++index)
                stringBuilder.Append(hash[index].ToString("x2"));

            return ((object) stringBuilder).ToString();
        }
    }
}
