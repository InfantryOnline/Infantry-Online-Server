using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AssetFetch.Helpers
{
    internal class Md5
    {
        /// <summary>
        /// Creates an MD5 has from a specific string
        /// </summary>
        public static string Hash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            return Hash(str, false);
        }

        /// <summary>
        /// Creates an MD5 hash from a specific file or string
        /// </summary>
        public static string Hash(string str, bool isFile)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            byte[] hash;
            if (isFile)
            {
                if (!File.Exists(str))
                    return null;
                FileStream fileStream = new FileStream(str, FileMode.Open);
                hash = MD5.Create().ComputeHash(fileStream);
                fileStream.Close();
            }
            else
            { hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(str)); }

            StringBuilder stringBuilder = new StringBuilder();

            for (int index = 0; index < hash.Length; index++)
                stringBuilder.Append(hash[index].ToString("x2"));

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Creates an MD5 hash from an array of bytes
        /// </summary>
        public static string Hash(byte[] bytes)
        {
            var hash = MD5.Create().ComputeHash(bytes);
            StringBuilder stringBuilder = new StringBuilder();

            for (int index = 0; index < hash.Length; index++)
                stringBuilder.Append(hash[index].ToString("x2"));

            return stringBuilder.ToString();
        }
    }
}