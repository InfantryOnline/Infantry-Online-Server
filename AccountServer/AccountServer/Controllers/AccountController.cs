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
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult Create(FormCollection collection)
        {
            var registration = new Account.RegistrationRequestModel();

            // Is the data valid?
            if(!registration.TryParseForm(collection))
            {
                // Return a response informing the client that the request did not go through.
            }

            // Is the account already registered?
            Account a = _dbClient.AccountCreate(registration.Username, registration.Password, Guid.NewGuid().ToString(),
                                                DateTime.Now, DateTime.Now, 0);

            if (null == a)
            {
                Response.StatusCode = (int)HttpStatusCode.ResetContent;
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.Created;
            }

            return new EmptyResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {
            var login = new Account.LoginRequestModel();

            // Is the data valid?
            if(!login.TryParseForm(collection))
            {
                // Return a response informing the client that the request did not go through.
            }

            // Account non-existing / the credentials incorrect?
            Account a = _dbClient.AccountLogin(login.Username, login.Password);

            // Oh uh! Account not found? Invalid credentials?
            if(null == a)
            {
                Response.StatusCode = (int) HttpStatusCode.ResetContent;
                return new EmptyResult();
            }

            // Everything OK! Send the account information!
            var responseData = new Account.LoginResponseModel {SessionId = a.SessionId};

            var result = new JsonResult {Data = responseData};
            return result;
        }
    }
}
