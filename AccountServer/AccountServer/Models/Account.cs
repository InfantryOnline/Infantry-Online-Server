using System;
using System.Web.Mvc;

namespace AccountServer.Models
{
    public class Account
    {
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

            /// <summary>
            /// Returns true if the values are correctly parsed from the form; false otherwise.
            /// </summary>
            /// <param name="form"></param>
            /// <returns></returns>
            public bool TryParseForm(FormCollection form)
            {
                if(form == null)
                    throw new ArgumentNullException("form");

                Username = form["username"];
                Password = form["password"];

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

                Username = form["username"];
                Password = form["password"];

                return true;
            }
        }

        public class LoginResponseModel
        {
            public Guid SessionId { get; set; }
        }

        #endregion
    }
}