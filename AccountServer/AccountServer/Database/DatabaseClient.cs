using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using AccountServer.Models;

namespace AccountServer.Database
{
    /// <summary>
    /// Preliminary database client.
    /// </summary>
    public class DatabaseClient
    {
        private SHA1CryptoServiceProvider _cryptoProvider;
        private SqlConnection _connection;
        private string _connString;

        #region Stored Proc strings

        private String _strLatestIdentity = "SELECT @@IDENTITY";

        private String _strCreateAccount =
            "INSERT INTO account (name, password, ticket, dateCreated, lastAccess, permission) VALUES " +
            "(@name, @password, @ticket, @dateCreated, @lastAccess, @permission)";

        private String _strUsernameExists = "SELECT * FROM account WHERE name LIKE @name";

        private String _strAccountValid = "SELECT * FROM account WHERE name LIKE @name AND password LIKE @password";

        #endregion

        public DatabaseClient()
        {
            _connString =
                "Data Source=localhost; Initial Catalog=Infantry; Trusted_Connection=True;";

            _connection = new SqlConnection(_connString);

            _connection.Open();
        }

        public Account AccountCreate(string username, string password, string ticket, DateTime dateCreated, DateTime lastAccess, int permission)
        {
            var _createAccountCmd = new SqlCommand(_strCreateAccount, _connection);

            _createAccountCmd.Parameters.AddWithValue("@name", username);
            _createAccountCmd.Parameters.AddWithValue("@password", password);
            _createAccountCmd.Parameters.AddWithValue("@ticket", ticket);
            _createAccountCmd.Parameters.AddWithValue("@dateCreated", dateCreated);
            _createAccountCmd.Parameters.AddWithValue("@lastAccess", lastAccess);
            _createAccountCmd.Parameters.AddWithValue("@permission", permission);

            if(_createAccountCmd.ExecuteNonQuery() != 1)
            {
                return null;
            }

            return new Account
                       {
                           DateCreated = dateCreated,
                           LastAccessed = lastAccess,
                           SessionId = Guid.Parse(ticket),
                           Username = username,
                           Password = password,
                           Permission = permission
                       };
        }

        public bool UsernameExists(string username)
        {
            var cmd = new SqlCommand(_strUsernameExists, _connection);

            cmd.Parameters.AddWithValue("@name", username);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }

            return true;
        }

        public bool IsAccountValid(string username, string password)
        {
            var cmd = new SqlCommand(_strAccountValid, _connection);

            cmd.Parameters.AddWithValue("@name", username);
            cmd.Parameters.AddWithValue("@password", password);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }

            return true;
        }

        public Account AccountLogin(string username, string password)
        {
            var cmd = new SqlCommand(_strAccountValid, _connection);

            cmd.Parameters.AddWithValue("@name", username);
            cmd.Parameters.AddWithValue("@password", password);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return null;

                reader.Read();

                return new Account
                           {
                               Id = reader.GetInt64(0),
                               Username = reader.GetString(1),
                               Password = reader.GetString(2),
                               SessionId = Guid.Parse(reader.GetString(3)),
                               DateCreated = reader.GetDateTime(4),
                               LastAccessed = reader.GetDateTime(5),
                               Permission = reader.GetInt32(6)
                           };
            }
        }
    }
}