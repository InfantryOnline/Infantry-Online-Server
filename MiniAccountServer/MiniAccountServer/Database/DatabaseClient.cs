using System;
using System.Data.SqlClient;
using System.Xml;
using MiniAccountServer.Models;
using MiniAccountServer.Helpers;
using MiniAccountServer.Helpers.Config;
using System.Net.Mail;

namespace MiniAccountServer.Database
{
    /// <summary>
    /// Preliminary database client.
    /// </summary>
    public class DatabaseClient
    {
        ConfigSetting _config;
        private SqlConnection _connection;
        private string _connString;

        #region Stored Proc strings

        private String _strLatestIdentity = "SELECT @@IDENTITY";

        private String _strCreateAccount =
            "INSERT INTO account (name, password, ticket, dateCreated, lastAccess, permission, email) VALUES " +
            "(@name, @password, @ticket, @dateCreated, @lastAccess, @permission, @email)";

        private String _strUsernameExists = "SELECT * FROM account WHERE name LIKE @name";

        private String _strAccountValid = "SELECT * FROM account WHERE name LIKE @name AND password LIKE @password";

        private String _strLoginUpdate =
            "UPDATE account SET ticket=@ticket WHERE name LIKE @name;" +
            "UPDATE account SET lastAccess=@time WHERE name LIKE @name;" +
            "UPDATE account SET IPAddress=@ipaddress WHERE name LIKE @name";

        private String _strPasswordUpdate =
            "UPDATE account SET password=@password WHERE name LIKE @name";

        private String _strEmailExists = "SELECT * FROM account WHERE email LIKE @email";

        private String _strCreateToken =
            "INSERT INTO resetToken (account, name, token, expireDate, tokenUsed) VALUES " +
            "(@account, @name, @token, @expireDate, @tokenUsed)";

        private String _strTokenUserExists = "SELECT * FROM resetToken WHERE name LIKE @name";

        private String _strTokenExists = "SELECT * FROM resetToken WHERE token LIKE @token";

        private String _strTokenUpdate = 
            "UPDATE resetToken SET token=@token WHERE name LIKE @name;" +
            "UPDATE resetToken SET expireDate=@expireDate WHERE name LIKE @name;" +
            "UPDATE resetToken SET tokenUsed=@tokenUsed WHERE name LIKE @name";

        private String _strMarkTokenUsed =
            "UPDATE resetToken SET tokenUsed=@tokenUsed WHERE token LIKE @token";

        #endregion

        /// <summary>
        /// Creates our client then opens a connection to our database
        /// </summary>
        public DatabaseClient()
        {
            _connString = "Server=localhost\\SQLEXPRESS;Database=Data;Trusted_Connection=True;";

            _connection = new SqlConnection(_connString);

            _connection.Open();
        }

        /// <summary>
        /// Creates an account in our database and returns the parsed account object
        /// </summary>
        public Account AccountCreate(string username, string password, string ticket, DateTime dateCreated, DateTime lastAccess, int permission, string email)
        {
            if (UsernameExists(username))
            {
                return null;
            }

            var _createAccountCmd = new SqlCommand(_strCreateAccount, _connection);

            _createAccountCmd.Parameters.AddWithValue("@name", username);
            _createAccountCmd.Parameters.AddWithValue("@password", password);
            _createAccountCmd.Parameters.AddWithValue("@ticket", ticket);
            _createAccountCmd.Parameters.AddWithValue("@dateCreated", dateCreated);
            _createAccountCmd.Parameters.AddWithValue("@lastAccess", lastAccess);
            _createAccountCmd.Parameters.AddWithValue("@permission", permission);
            _createAccountCmd.Parameters.AddWithValue("@email", email);

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
                           Permission = permission,
                           Email = email
                       };
        }

        /// <summary>
        /// Does the username exist
        /// </summary>
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

        /// <summary>
        /// Does the username and password match our records
        /// </summary>
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

        /// <summary>
        /// Does the email exist
        /// </summary>
        public bool EmailExists(string email)
        {
            var cmd = new SqlCommand(_strEmailExists, _connection);

            cmd.Parameters.AddWithValue("@email", email);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tries logging in using the given username and pass and returns a parsed account object
        /// </summary>
        public Account AccountLogin(string username, string password, string IPAddress)
        {
            //Update some stuff first, mang
            var update = new SqlCommand(_strLoginUpdate, _connection);

            update.Parameters.AddWithValue("@name", username);
            update.Parameters.AddWithValue("@ticket", Guid.NewGuid().ToString());
            update.Parameters.AddWithValue("@time", DateTime.Now);
            update.Parameters.AddWithValue("@ipaddress", IPAddress);

            update.ExecuteNonQuery();
            
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
                               Permission = reader.GetInt32(6),
                               Email = reader.GetString(7),
                           };
            }
        }

        /// <summary>
        /// Does our token match?
        /// </summary>
        public bool IsTokenValid(string token)
        {
            string test = string.Empty;
            var cmd = new SqlCommand(_strTokenExists, _connection);

            cmd.Parameters.AddWithValue("@token", token);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                reader.Read();
                reader.GetInt64(0); //ID
                reader.GetInt64(1); //Account ID
                reader.GetString(2); //Name
                test = reader.GetString(3); //Token

                reader.Close();
            }

            if (token.Equals(test))
                return true;

            return false;
        }

        /// <summary>
        /// Does the user exist in our reset token records
        /// </summary>
        public bool TokenUsernameExists(string username)
        {
            var cmd = new SqlCommand(_strTokenUserExists, _connection);
            cmd.Parameters.AddWithValue("@name", username);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Was this token already used?
        /// </summary>
        public bool TokenUsed(string token)
        {
            bool tokenUsed = false;

            var cmd = new SqlCommand(_strTokenExists, _connection);

            cmd.Parameters.AddWithValue("@token", token);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                reader.Read();
                reader.GetInt64(0); //ID
                reader.GetInt64(1); //Account ID
                reader.GetString(2); //Name
                reader.GetString(3); //Token
                reader.GetDateTime(4); //Expire Date
                tokenUsed = reader.GetBoolean(5); //Token Used

                reader.Close();
            }

            return tokenUsed;
        }

        /// <summary>
        /// Did the token expire?
        /// </summary>
        public bool TokenExpired(string token)
        {
            DateTime expireDate;

            var cmd = new SqlCommand(_strTokenExists, _connection);

            cmd.Parameters.AddWithValue("@token", token);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                reader.Read();
                reader.GetInt64(0); //ID
                reader.GetInt64(1); //Account ID
                reader.GetString(2); //Name
                reader.GetString(3); //Token
                expireDate = reader.GetDateTime(4); //Expire Date

                reader.Close();
            }

            if (DateTime.Now >= expireDate)
                return true;

            return false;
        }

        /// <summary>
        /// Recovers a users account name
        /// </summary>
        public bool AccountRecover(string email, out string username)
        {
            username = string.Empty;

            var cmd = new SqlCommand(_strEmailExists, _connection);
            cmd.Parameters.AddWithValue("@email", email);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                reader.Read();

                reader.GetInt64(0); //ID
                username = reader.GetString(1);

                reader.Close();
            }

            if (!string.IsNullOrWhiteSpace(username))
                return true;

            return false;
        }

        /// <summary>
        /// Called upon to create a password reset token, outputs email and generated token
        /// </summary>
        public bool AccountReset(string username, out string[] parameters)
        {
            parameters = null;
            string email = null;
            long accountID;
            bool updating = false;

            //Lets try getting the account id and generate a token first
            var cmd = new SqlCommand(_strUsernameExists, _connection);

            cmd.Parameters.AddWithValue("@name", username);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                reader.Read();

                accountID = reader.GetInt64(0); //ID
                reader.GetString(1); //Name
                reader.GetString(2); //Password
                reader.GetString(3); //Session ID
                reader.GetDateTime(4); //Date Created
                reader.GetDateTime(5); //Last Accessed
                reader.GetInt32(6); //Server Permission
                email = reader.GetString(7); //Email

                reader.Close();
            }

            if (email == null)
                return false;

            //Lets generate
            string token = Crypto.GenerateToken();
            if (string.IsNullOrEmpty(token))
                return false;

            parameters = new string[] { email, token };

            //Do we have a user created already?
            if (TokenUsernameExists(username))
            {   //We have one
                //Check for a pending request first
                DateTime tokenDate;
                bool tokenUsed;
                string oldToken = string.Empty;
                cmd = new SqlCommand(_strTokenUserExists, _connection);

                cmd.Parameters.AddWithValue("@name", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return false;

                    reader.Read();
                    reader.GetInt64(0); //ID
                    reader.GetInt64(1); //Account ID
                    reader.GetString(2); //Name
                    oldToken = reader.GetString(3); //Token
                    tokenDate = reader.GetDateTime(4); //Expire Date
                    tokenUsed = reader.GetBoolean(5); //Token Used

                    reader.Close();
                }

                //If the token hasnt been used and hasnt expired
                //then just resend the old token
                if (!string.IsNullOrEmpty(oldToken) && 
                    tokenDate > DateTime.Now && !tokenUsed)
                {
                    parameters = new string[] { email, oldToken };
                    return true;
                }
                updating = true;
            }
            else
            {   //Creating
                //Save the token to the database
                cmd = new SqlCommand(_strCreateToken, _connection);

                cmd.Parameters.AddWithValue("@account", accountID);
                cmd.Parameters.AddWithValue("@name", username);
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@expireDate", DateTime.Now.AddHours(48));
                cmd.Parameters.AddWithValue("@tokenUsed", false);
                try
                {
                    if (cmd.ExecuteNonQuery() != 1)
                        return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return false;
                }
            }

            if (updating)
            {   //Updating
                //We had one but it expired, lets reuse the data structure
                var update = new SqlCommand(_strTokenUpdate, _connection);

                update.Parameters.AddWithValue("@name", username);
                update.Parameters.AddWithValue("@token", token);
                update.Parameters.AddWithValue("@expireDate", DateTime.Now.AddHours(48));
                update.Parameters.AddWithValue("@tokenUsed", false);

                try
                {
                    update.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Updates a users password after being reset
        /// </summary>
        public bool AccountPasswordUpdate(string token, string password)
        {
            string username = null;

            //Get the username first
            var cmd = new SqlCommand(_strTokenExists, _connection);

            cmd.Parameters.AddWithValue("@token", token);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                reader.Read();
                reader.GetInt64(0); //ID
                reader.GetInt64(1); //Account ID
                username = reader.GetString(2); //Name

                reader.Close();
            }

            if (string.IsNullOrEmpty(username))
                return false;

            //Does the username exist in the account structure?
            if (!UsernameExists(username))
                return false;

            //Did it encrypt correctly?
            if ((password = Crypto.Hash(password)) == null)
                return false;

            //Update the password
            var update = new SqlCommand(_strPasswordUpdate, _connection);

            update.Parameters.AddWithValue("@name", username);
            update.Parameters.AddWithValue("@password", password);

            try
            {
                update.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            //Finally, update the token as used and save
            update = new SqlCommand(_strMarkTokenUsed, _connection);

            update.Parameters.AddWithValue("@token", token);
            update.Parameters.AddWithValue("@tokenUsed", true);

            try
            {
                update.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends a recovery email to a specified email
        /// </summary>
        public bool AccountSendMail(string email, string username, string token)
        {
            if (!System.IO.File.Exists("email.xml"))
                return false;

            try
            {
                _config = new Xmlconfig("email.xml", false).Settings;
                string host = _config["credentials/hostname"].Value;
                string from = _config["credentials/email"].Value;
                string credentialUsername = _config["credentials/username"].Value;
                string to = email;

                var fromAddress = new MailAddress(from, "FreeInfantry");
                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = fromAddress;
                mail.To.Add(to);
                mail.Subject = "Recovery Request";
                mail.Body = _config["response/topic"].Value + "\n\r";
                //Recovery or reset?
                if (!string.IsNullOrWhiteSpace(token))
                {   //Reset
                    mail.Body += _config["response/resetmsg"].Value;
                    mail.Body += "\n\r";
                    mail.Body += _config["response/link"].Value + System.Web.HttpUtility.UrlEncode(token);
                }
                else
                {   //Recovery
                    mail.Body += string.Format(_config["response/recoverymsg"].Value, username);
                }

                mail.Body += "\n\r\n\r" + _config["response/endmsg"].Value;

                System.Net.Mail.SmtpClient smtpClient = new System.Net.Mail.SmtpClient(host);
                smtpClient.Port = _config["credentials/port"].intValue;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new System.Net.NetworkCredential(credentialUsername, _config["credentials/password"].Value);

                smtpClient.Send(mail);
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Parses the email and only shows a select amount of it for the response
        /// </summary>
        public string EncodeEmail(string email)
        {
            string response = string.Empty;
            string[] split = email.Split('@');

            if (split[0].Length > 3)
            {
                response = split[0].Substring(0, 4);
                for(int i = 0; i < split[0].Length; i++)
                {
                    //Skip the first 4
                    if (i < 4)
                    { continue; }
                    response += "*";
                }
                response += "@" + split[1];
            }
            else
            {
                for(int i = 0; i < split[0].Length; i++)
                {
                    response += "*";
                }
                response += "@" + split[1];
            }
            return response;
        }
    }
}