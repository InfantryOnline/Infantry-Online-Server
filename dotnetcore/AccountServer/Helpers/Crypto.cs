using System;
using System.Text;
using System.Security.Cryptography;

namespace AccountServer.Helpers
{
    public static class Crypto
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int Iterations = 600_000; // Current OWASP recommended minimum for PBKDF2-SHA256
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        public static (string Hash, string Salt) HashPassword(string password)
        {
            // 1. Generate a cryptographically secure random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // 2. Derive the byte array hash using PBKDF2
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySize);

            // 3. Convert both to Hex or Base64 strings for database storage
            return (Convert.ToHexString(hash), Convert.ToHexString(salt));
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] salt = Convert.FromHexString(storedSalt);
            byte[] hash = Convert.FromHexString(storedHash);

            // Re-hash the input password using the retrieved salt
            byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySize);

            // Use CryptographicOperations.FixedTimeEquals to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(hash, newHash);
        }

        /// <summary>
        /// Creates an MD5 has from a specific string
        /// </summary>
        public static string ComputeMd5Hash(string str)
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

        public static string ComputeSha256Hash(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                var sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
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
