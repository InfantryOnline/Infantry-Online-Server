using System;
using System.Collections.Generic;
using System.Linq;
using AccountServer.Models;

namespace AccountServer.Database
{
    /// <summary>
    /// Fake database used for testing when no valid database is available.
    /// </summary>
    public class MemoryDatabaseClient
    {
        /// <summary>
        /// The list of accounts.
        /// </summary>
        private static List<Account> _accounts = new List<Account>();

        private static long _idCounter = 0;

        public Account AccountCreate(string username, string password)
        {
            if (AccountExists(username, password) != null)
            {
                return null;
            }

            var a = new Account();

            a.DateCreated = DateTime.Now;
            a.LastAccessed = DateTime.Now;

            a.Username = username;
            a.Password = password;

            a.Id = _idCounter++;
            a.SessionId = Guid.NewGuid();

            a.Permission = 0;

            _accounts.Add(a);

            return a;
        }

        public Account AccountLogin(string username, string password)
        {
            Account a = AccountExists(username, password);

            return a;
        }

        private Account AccountExists(string username, string password)
        {
            foreach(var acc in _accounts)
            {
                if (acc.Username.Equals(username) && acc.Password.Equals(password))
                        return acc;
            }

            return null;
        }
    }
}