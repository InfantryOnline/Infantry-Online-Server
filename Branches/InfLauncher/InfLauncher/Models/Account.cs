using System;

namespace InfLauncher.Models
{
    public class Account
    {
        #region Validators

        /// <summary>
        /// Returns true if the username is at least four characters long.
        /// </summary>
        /// <param name="username">The username to verify</param>
        /// <returns>true if valid</returns>
        public static bool IsValidUsername(string username)
        {
            return username.Length >= 4;
        }

        /// <summary>
        /// Returns true if the email is valid.
        /// </summary>
        /// <param name="email">The email to verify</param>
        /// <returns>true if valid</returns>
        public static bool IsValidEmail(string email)
        {
            return email.Contains("@");
        }

        /// <summary>
        /// Returns true if the password is not left blank.
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <returns>true if valid</returns>
        public static bool IsValidPassword(string password)
        {
            return !String.IsNullOrWhiteSpace(password);
        }

        #endregion


        #region Account Request Models

        /// <summary>
        /// Account data sent for a request to login.
        /// </summary>
        public class AccountLoginRequestModel
        {
            /// <summary>
            /// Creates a new AccountLoginRequestModel object.
            /// </summary>
            /// <param name="username">The username associated with the requested account</param>
            /// <param name="passwordHash">The password associated with the requested account</param>
            public AccountLoginRequestModel(string username, string passwordHash)
            {
                Username = username;
                PasswordHash = passwordHash;
            }

            /// <summary>
            /// The username associated with this account.
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// The hashed password associated with this account.
            /// </summary>
            public string PasswordHash { get; set; }
        }

        /// <summary>
        /// Account data sent for a registration request.
        /// </summary>
        public class AccountRegistrationRequestModel
        {
            /// <summary>
            /// Creates a new AccountRegistrationRequestModel object.
            /// </summary>
            /// <param name="username">Username for the newly requested account</param>
            /// <param name="passwordHash">Password hash for the newly requested account</param>
            /// <param name="email">Email for the newly requested account</param>
            public AccountRegistrationRequestModel(string username, string passwordHash, string email)
            {
                Username = username;
                PasswordHash = passwordHash;
                Email = email;
            }

            /// <summary>
            /// The requested username.
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// The requested hashed password.
            /// </summary>
            public string PasswordHash { get; set; }

            /// <summary>
            /// The requested email.
            /// </summary>
            public string Email { get; set; }
        }

        #endregion


        #region Account Response Models

        /// <summary>
        /// Account data returned upon a login.
        /// </summary>
        public class AccountLoginResponseModel
        {
            /// <summary>
            /// Creates a new AccountLoginResponseModel object.
            /// </summary>
            /// <param name="username">The account's username</param>
            /// <param name="email">The account's email</param>
            /// <param name="ticketId">The account's unique ticket token</param>
            /// <param name="dateCreated">The date of account's creation</param>
            /// <param name="lastAccessed">The previous time the account was accessed</param>
            /// <param name="permission">Permission level of this account</param>
            public AccountLoginResponseModel(string username, string email, Guid ticketId, 
                DateTime dateCreated, DateTime lastAccessed, int permission)
            {
                Username = username;
                Email = email;
                TicketId = ticketId;
                DateCreated = dateCreated;
                LastAccessed = lastAccessed;
                Permission = permission;
            }

            /// <summary>
            /// The username associated with this account.
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// The email associated with this account.
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// The unique ticket associated with this account.
            /// </summary>
            public Guid TicketId { get; set; }

            /// <summary>
            /// Date of creation for this account.
            /// </summary>
            public DateTime DateCreated { get; set; }

            /// <summary>
            /// Last time the account has been accessed.
            /// </summary>
            public DateTime LastAccessed { get; set; }

            /// <summary>
            /// Permission level for this account.
            /// </summary>
            public int Permission { get; set; }
        }

        #endregion

    }
}
