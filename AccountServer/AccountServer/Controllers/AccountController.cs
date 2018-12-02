using System;
using System.Net;
using System.Web.Mvc;
using AccountServer.Database;
using AccountServer.Models;

namespace AccountServer.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class AccountController : Controller
    {
        private DatabaseClient _dbClient = new DatabaseClient();

        /// <summary>
        /// Registers a new account with the server.
        /// 
        /// 
        /// The client is expected to send a "PUT" request to the Create URL with the following form fields:
        /// 
        ///     username - Valid username
        ///     email    - Valid email
        ///     password - SHA1 password hash
        /// 
        /// 
        /// Only a response code is returned to the client:
        /// 
        ///     201 (Created)        -- The successful response; Account created.
        /// 
        ///     400 (Bad Request)    -- The form fields are missing or not in the right format.
        /// 
        ///     403 (Forbidden)      -- Account already exists.
        /// 
        ///     406 (Not Acceptable) -- Credentials are weak (username or email fail validation).
        /// 
        ///     500 (Internal Error) -- Issue with the server.
        /// </summary>
        /// <param name="collection">Form data passed in by the client</param>
        /// <returns>A response object for the client</returns>
        [HttpPut]
        public ActionResult Create(FormCollection collection)
        {
            var registration = new Account.RegistrationRequestModel();

            // 1. Try to parse the form data
            if(!registration.TryParseForm(collection))
            {
                Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return new EmptyResult();
            }

            // 2. Has the account already been registered?
            if(_dbClient.UsernameExists(registration.Username))
            {
                Response.StatusCode = (int) HttpStatusCode.Forbidden;
                return new EmptyResult();
            }

            // 3. Are the credentials good?
            if(!Account.IsValidUsername(registration.Username))
            {
                Response.StatusCode = (int) HttpStatusCode.NotAcceptable;
                return new EmptyResult();
            }

            // 4. Create!
            if(null != _dbClient.AccountCreate(registration.Username, registration.Password, 
                Guid.NewGuid().ToString(), DateTime.Now, DateTime.Now, 0, registration.Email))
            {
                Response.StatusCode = (int) HttpStatusCode.Created;
            }
            else
            {
                // Oh uh!
                Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Logins an account with the server.
        /// 
        /// 
        /// The client is expected to send a "POST" request to the Login URL with the following form fields:
        /// 
        ///     username - Valid username
        ///     password - SHA1 password hash
        /// 
        /// 
        /// A response code, including a JSON payload containing the Session Id is returned:
        /// 
        ///     200 (OK)             -- The successful response; account logged in.
        /// 
        ///     400 (Bad Request)    -- The form fields are missing or not in the right format.
        /// 
        ///     404 (Not Found)      -- The username or password are invalid.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {
            var login = new Account.LoginRequestModel();

            // 1. Try to parse form data
            if(!login.TryParseForm(collection))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new EmptyResult();
            }

            // 2. Account credentials good?
            Account a = _dbClient.AccountLogin(login.Username, login.Password);
            if(a == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return new EmptyResult();
            }

            // 4. Login and send the user their session id!
            var responseData = new Account.LoginResponseModel {TicketId = a.SessionId};
            var result = new JsonResult {Data = responseData};
            return result;
        }
    }
}
