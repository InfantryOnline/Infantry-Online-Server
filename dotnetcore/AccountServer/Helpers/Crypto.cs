using System;
using System.Text;
using System.Security.Cryptography;

namespace AccountServer.Helpers
{
    public static class Crypto
    {
        /// <summary>
        /// Creates an MD5 has from a specific string
        /// </summary>
        public static string Hash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            
            byte[] hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(str));
            StringBuilder stringBuilder = new StringBuilder();

            for (int index = 0; index < hash.Length; index++)
                stringBuilder.Append(hash[index].ToString("x2"));

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generates a token and returns an 8bit version
        /// </summary>
        /// <returns></returns>
        public static string GenerateToken()
        {
            Guid guid = System.Guid.NewGuid();
            byte[] crypted = ComputeSHA1Hash(guid.ToByteArray());
            crypted = ExtractAndReverse(crypted);

            return Convert.ToBase64String(crypted);
        } 
        
        /// <summary>
        /// Computes a byte array into SHA1 Managed format
        /// </summary>
        private static byte[] ComputeSHA1Hash(byte[] publicKey)
        {
            SHA1Managed sha1 = new SHA1Managed();
            byte[] hash = sha1.ComputeHash(publicKey);
            return hash;
        }

        /// <summary>
        /// Extracts 8 bytes from the hashed token then reverses it
        /// </summary>
        private static byte[] ExtractAndReverse(byte[] hash)
        {
            byte[] publicToken = new byte[8]; //We only want the last 8 bytes
            Array.Copy(hash, hash.Length - publicToken.Length, publicToken, 0, publicToken.Length);

            //Reverse it
            Array.Reverse(publicToken, 0, publicToken.Length);
            return publicToken;
        }
    }
}
