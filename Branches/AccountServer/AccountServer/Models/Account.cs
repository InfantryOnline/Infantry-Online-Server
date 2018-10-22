using System;
using System.Web.Mvc;

namespace AccountServer.Models
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

        #endregion


        #region Database Fields

        /// <summary>
        /// Unique identifier for this account.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Account's username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Account's hashed password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Account's email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Secret Session Id for this account.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Account's creation time.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Last time the account was used.
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// Zone/Server permission flags.
        /// </summary>
        public int Permission { get; set; }

        #endregion


        #region HTTP Response/Request models

        public class RegistrationRequestModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }

            /// <summary>
            /// Returns true if the values are correctly parsed from the form; false otherwise.
            /// </summary>
            /// <param name="form"></param>
            /// <returns></returns>
            public bool TryParseForm(FormCollection form)
            {
                if(form == null)
                    throw new ArgumentNullException("form");

                try
                {
                    Username = form["username"];
                    Password = form["password"];
                    Email = form["email"];
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }

        public class LoginRequestModel
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public bool TryParseForm(FormCollection form)
            {
                if(form == null)
                    throw new ArgumentNullException("form");

                try
                {
                    Username = form["username"];
                    Password = form["password"];
                }
                catch
                {
                    return false;
                }
                

                return true;
            }
        }

        public class LoginResponseModel
        {
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