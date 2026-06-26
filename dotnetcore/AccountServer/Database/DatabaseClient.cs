using System;
using System.Data.SqlClient;
using System.Xml;
using AccountServer.Models;
using AccountServer.Helpers;
using System.Net.Mail;
using InfServer;
using InfServer.Protocol;
using Database.SqlServer;

using Account = AccountServer.Models.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Database;
using Database.Sqlite;

namespace AccountServer
{
    /// <summary>
    /// Preliminary database client.
    /// </summary>
    public class DatabaseClient
    {
        ConfigSetting _config;
        private IDbContextFactory<InfantryDbContext> _dbContextFactory;

        /// <summary>
        /// Creates our client then opens a connection to our database
        /// </summary>
        public DatabaseClient()
        {
            _config = new Xmlconfig("server.xml", false).Settings;

            var _connString = _config["database/connectionString"].Value;
            var _sqliteString = _config["database/sqlite"].Value;

            if (!string.IsNullOrWhiteSpace(_connString))
            {
                Console.WriteLine("Connecting to SQL Server...");

                var options = new DbContextOptionsBuilder<SqlServerDbContext>()
                    .UseSqlServer(_connString)
                    .Options;

                var pooledFact = new PooledDbContextFactory<SqlServerDbContext>(options);

                _dbContextFactory = new InfantryDbFactory<SqlServerDbContext>(pooledFact);
            }
            else if (!string.IsNullOrWhiteSpace(_sqliteString))
            {
                Console.WriteLine("Connecting to SQLite...");

                var options = new DbContextOptionsBuilder<SqliteDbContext>()
                    .UseSqlite(_sqliteString)
                    .Options;

                var pooledFact = new PooledDbContextFactory<SqliteDbContext>(options);

                _dbContextFactory = new InfantryDbFactory<SqliteDbContext>(pooledFact);
            }
            else
            {
                throw new ApplicationException("No connection string to database found.");
            }
        }
        
        public string GetHealthCheck()
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var accounts = ctx.Accounts.Count();
                var zones = ctx.Zones.Count();

                return $"Accounts: {accounts}, Zones: {zones}";
            }
        }

        /// <summary>
        /// Creates an account in our database and returns the parsed account object
        /// </summary>
        public Account? AccountCreate(string username, string password, string ticket, DateTime dateCreated, DateTime lastAccess, int permission, string email)
        {
            if (UsernameExists(username))
            {
                return null;
            }

            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var acct = new Database.Account
                {
                    Name = username,
                    Password = password,
                    Ticket = ticket,
                    DateCreated = dateCreated,
                    LastAccess = lastAccess,
                    Permission = permission,
                    Email = email
                };

                ctx.Accounts.Add(acct);

                ctx.SaveChanges();

                return new Account
                {
                    Id = acct.AccountId,
                    DateCreated = dateCreated,
                    LastAccessed = lastAccess,
                    SessionId = Guid.Parse(ticket),
                    Username = username,
                    Password = password,
                    Permission = permission,
                    Email = email
                };
            }
        }

        /// <summary>
        /// Does the username exist
        /// </summary>
        public bool UsernameExists(string username)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                return ctx.Accounts.Any(a => a.Name == username);
            }
        }

        /// <summary>
        /// Does the username and password match our records
        /// </summary>
        public bool IsAccountValid(string username, string passwordMd5Hash)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var account = ctx.Accounts.FirstOrDefault(a => a.Name == username);

                if (account == null)
                {
                    return false;
                }

                var parts = account.Password.Split('.');

                if (parts.Length == 2)
                {
                    return Crypto.VerifyPassword(passwordMd5Hash, parts[0], parts[1]);
                }

                // Old-style SHA256 passwords.
                return account.Password == Crypto.ComputeSha256Hash(passwordMd5Hash);
            }
        }

        /// <summary>
        /// Does the email exist
        /// </summary>
        public bool EmailExists(string email)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                return ctx.Accounts.Any(a => a.Email == email);
            }
        }

        /// <summary>
        /// Tries logging in using the given username and pass and returns a parsed account object
        /// </summary>
        public Account? AccountLogin(string username, string passwordMd5Hash, string IPAddress)
        {
            if (!IsAccountValid(username, passwordMd5Hash))
            {
                return null;
            }

            TryUpgradePassword(username, passwordMd5Hash);

            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var acct = ctx.Accounts.First(s => s.Name == username);

                var ticket = Guid.NewGuid();

                acct.Ticket = ticket.ToString();
                acct.LastAccess = DateTime.Now;
                acct.IpAddress = IPAddress;

                ctx.SaveChanges();

                return new Account
                {
                    Id = acct.AccountId,
                    Username = acct.Name,
                    Password = acct.Password, // TODO: I wouldn't return the password or have it be exposed in Account model at all.
                    SessionId = ticket,
                    DateCreated = acct.DateCreated,
                    LastAccessed = acct.LastAccess,
                    Permission = acct.Permission,
                    Email = acct.Email,
                };
            }
        }

        /// <summary>
        /// Does our token match?
        /// </summary>
        public bool IsResetTokenValid(string token)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                return ctx.ResetTokens.Any(t => t.Token == token);
            }
        }

        /// <summary>
        /// Was this token already used?
        /// </summary>
        public bool WasTokenUsed(string token)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                return ctx.ResetTokens.Any(t => t.Token == token && t.TokenUsed == true);
            }
        }

        /// <summary>
        /// Did the token expire?
        /// </summary>
        public bool HasTokenExpired(string token)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                return ctx.ResetTokens.Any(t => t.Token == token && t.ExpireDate <= DateTime.Now);
            }
        }

        /// <summary>
        /// Recovers a users account name
        /// </summary>
        public bool TryGetUsernameFromEmail(string email, out string username)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var acct = ctx.Accounts.FirstOrDefault(a => a.Email == email);

                if (acct == null)
                {
                    username = string.Empty;
                    return false;
                }

                username = acct.Name;
                return true;
            }
        }

        /// <summary>
        /// Called upon to create a password reset token. Outputs email and generated token
        /// </summary>
        public bool TryGenerateTokenForAccountReset(string username, out string email, out string token)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var acct = ctx.Accounts.FirstOrDefault(a => a.Name == username);

                if (acct == null)
                {
                    email = "";
                    token = "";

                    return false;
                }

                token = Crypto.GenerateToken();
                email = acct.Email;

                var existingToken = ctx.ResetTokens.FirstOrDefault(t => t.Name == username);

                if (existingToken != null)
                {
                    // Token exists, but it may have been used or expired. In both cases,
                    // we will re-use the existing record and just update it.

                    existingToken.TokenUsed = false;
                    existingToken.ExpireDate = DateTime.Now.AddHours(48);
                    existingToken.Token = token;
                }
                else
                {
                    var rt = new ResetToken
                    {
                        Name = username,
                        AccountId = acct.AccountId,
                        Token = token,
                        ExpireDate = DateTime.Now.AddHours(48),
                        TokenUsed = false,
                    };

                    ctx.ResetTokens.Add(rt);
                }

                ctx.SaveChanges();

                return true;
            }
        }

        /// <summary>
        /// Updates a users password after being reset
        /// </summary>
        public bool TryUpdatePasswordWithToken(string token, string passwordMd5Hash)
        {
            if (string.IsNullOrWhiteSpace(passwordMd5Hash))
            {
                return false;
            }

            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var rt = ctx.ResetTokens.Include(r => r.AccountNavigation).FirstOrDefault(t => t.Token == token);

                var parts = Crypto.HashPassword(passwordMd5Hash);

                rt.TokenUsed = true;
                rt.AccountNavigation.Password = $"{parts.Hash}.{parts.Salt}";

                ctx.SaveChanges();
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

        private void TryUpgradePassword(string username, string passwordMd5Hash)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var account = ctx.Accounts.FirstOrDefault(a => a.Name == username);

                if (account == null)
                {
                    return;
                }

                var parts = account.Password.Split('.');

                if (parts.Length == 2)
                {
                    // Already upgraded
                    return;
                }

                var output = Crypto.HashPassword(passwordMd5Hash);

                account.Password = $"{output.Hash}.{output.Salt}";
                ctx.SaveChanges();
            }
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
