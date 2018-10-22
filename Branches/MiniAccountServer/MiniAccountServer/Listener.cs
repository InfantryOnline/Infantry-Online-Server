using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MiniAccountServer.Database;

using MiniAccountServer.Models;
using Newtonsoft.Json;
using System.Web;

namespace MiniAccountServer
{
    public class Listener
    {
        private HttpListener httpListener;
        private DatabaseClient client;

        private string[] prefixes = {@"http://0.0.0.0:1437/Account/"};

        /// <summary>
        /// Generic Constructor
        /// </summary>
        public Listener()
        {
            client = new DatabaseClient();
            httpListener = new HttpListener();

            httpListener.Prefixes.Add("http://*:1010/");
        }

        /// <summary>
        /// Starts our program, exits if any errors occur
        /// </summary>
        public void Start()
        {
            //Is this OS supported?
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener: Not supported on current system.");
                System.Threading.Thread.Sleep(5000);
            }
            else
            {
                try
                {
                    httpListener.Start();
                    System.Threading.Thread.Sleep(1000);

                    //Are we activated?
                    if (!httpListener.IsListening)
                    {
                        Console.WriteLine("Cannot start HttpListener... Exiting.");
                        System.Threading.Thread.Sleep(5000);
                        return;
                    }

                    Console.WriteLine("Listening....");
                    while (httpListener.IsListening)
                    {
                        HttpListenerContext context = httpListener.GetContext();
                        HandleRequest(context);
                    }
                }
                catch (Exception e)
                {
                    httpListener.Close();

                    Console.WriteLine(e.ToString());
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            Console.WriteLine("Request {0}/{1} from {2}", request.RawUrl.ToString(), request.HttpMethod, request.RemoteEndPoint);
            try
            {
                // Lets figure out the request...
                switch (request.HttpMethod)
                {
                    // Sanity check request
                    case "GET":
                        byte[] responseString;

                        //We verifying a password reset token?
                        if (request.Url.AbsolutePath.Contains("verify-token"))
                        {
                            string tokenData = request.QueryString["token"];

                            response.AppendHeader("Access-Control-Allow-Origin", "*");
                            response.AppendHeader("Access-Control-Allow-Methods", "POST, GET");

                            //1. Does the token exist?
                            if (!client.IsTokenValid(tokenData))
                            {
                                responseString = Encoding.UTF8.GetBytes("false");
                                response.StatusCode = 404; //Not Found

                                response.ContentLength64 = responseString.Length;
                                response.OutputStream.Write(responseString, 0, responseString.Length);
                                response.OutputStream.Close();
                                
                                break;
                            }

                            //2. Did the token expire?
                            if (client.TokenExpired(tokenData))
                            {
                                responseString = Encoding.UTF8.GetBytes("false");
                                response.StatusCode = 400; //Bad Request

                                response.ContentLength64 = responseString.Length;
                                response.OutputStream.Write(responseString, 0, responseString.Length);
                                response.OutputStream.Close();
                                
                                break;
                            }

                            //3. Was the token used already?
                            if (client.TokenUsed(tokenData))
                            {
                                responseString = Encoding.UTF8.GetBytes("false");
                                response.StatusCode = 409; //Conflict

                                response.ContentLength64 = responseString.Length;
                                response.OutputStream.Write(responseString, 0, responseString.Length);
                                response.OutputStream.Close();
                                
                                break;
                            }

                            responseString = Encoding.UTF8.GetBytes("true");
                            response.StatusCode = 200; //Ok

                            response.ContentLength64 = responseString.Length;
                            response.OutputStream.Write(responseString, 0, responseString.Length);
                            response.OutputStream.Close();

                            break;
                        }

                        //Ping
                        responseString = Encoding.UTF8.GetBytes("Works!");
                        response.ContentLength64 = responseString.Length;
                        response.OutputStream.Write(responseString, 0, responseString.Length);
                        response.OutputStream.Close();

                        break;

                    // Account registration request
                    case "PUT":
                        // 1. Is the request good?
                        if (!request.HasEntityBody)
                        {
                            response.StatusCode = 400; //BadRequest
                            response.OutputStream.Close();

                            break;
                        }

                        string registrationData = new StreamReader(request.InputStream).ReadToEnd();
                        var regModel = JsonConvert.DeserializeObject<Account.RegistrationRequestModel>(registrationData);
                        // 2. Data valid?
                        if (!regModel.IsRequestValid())
                        {
                            response.StatusCode = 400; //BadRequest
                            response.OutputStream.Close();

                            break;
                        }

                        // 3. Is the username available?
                        if (client.UsernameExists(regModel.Username))
                        {
                            response.StatusCode = 403; //Forbidden
                            response.StatusDescription = "Username already exists.";
                            response.OutputStream.Close();

                            break;
                        }

                        // 4. Are the credentials good?
                        if (!Account.IsValidUsername(regModel.Username) || !Account.IsValidEmail(regModel.Email))
                        {
                            response.StatusCode = 406; //Not Acceptable
                            response.StatusDescription = (!Account.IsValidUsername(regModel.Username) ? "Invalid Username (Must be at least 4 characters long.)" : "Invalid Email");
                            response.OutputStream.Close();

                            break;
                        }
                        
                        // 5. Is the email already used?
			            if (client.EmailExists(regModel.Email))
			            {
			                response.StatusCode = 409; //Conflict
                            response.StatusDescription = "Email already exists.";
			                response.OutputStream.Close();
					
			                break;
		        	    }
						
                        // Add it to the database, and we're good to go!
                        var account = client.AccountCreate(regModel.Username, regModel.PasswordHash,
                                                           Guid.NewGuid().ToString(),
                                                           DateTime.Now, DateTime.Now, 0, regModel.Email);


                        // 6. Oh uh? Some error happened!
                        if (account == null)
                        {
                            response.StatusCode = 500; //Internal Server Error
                            response.StatusDescription = "Account Creation Failed.";
                            response.OutputStream.Close();

                            break;
                        }

                        // Done!
                        response.StatusCode = 201; //Created
                        response.OutputStream.Close();
                        break;

                    // Account login request
                    case "POST":
                        // 1. Is the request good?
                        if (!request.HasEntityBody)
                        {
                            response.StatusCode = 400; //BadRequest
                            response.OutputStream.Close();

                            break;
                        }

                        //Are we doing a recovery request?
                        if (request.Url.AbsolutePath.Contains("recover"))
                        {
                            byte[] RequestResponseData;
                            string emailResponse;
                            string resetData = new StreamReader(request.InputStream).ReadToEnd();
                            var resetModel = JsonConvert.DeserializeObject<Account.RecoverRequestModel>(resetData);

                            // 2. Is the data valid?
                            if (!resetModel.IsRequestValid())
                            {
                                response.StatusCode = 400; //BadRequest
                                response.StatusDescription = (resetModel.Reset ? "Username " : "Email ") + "cannot be blank.";
                                response.OutputStream.Close();

                                break;
                            }

                            //Username Recovery
                            if (!resetModel.Reset)
                            {
                                // 3a. Are the credentials good?
                                if (resetModel.Email != null && !client.EmailExists(resetModel.Email))
                                {
                                    response.StatusCode = 404; //Not Found
                                    response.StatusDescription = "Email doesn't exist.";
                                    response.OutputStream.Close();

                                    break;
                                }

                                // 3b. Are the credentials good?
                                if (!Account.IsValidEmail(resetModel.Email))
                                {
                                    response.StatusCode = 400; //Bad Request
                                    response.StatusDescription = "Incorrect email format. Ex: me@mydomain.com";
                                    response.OutputStream.Close();

                                    break;
                                }

                                // 4a. Was it successful?
                                string username;
                                if (!client.AccountRecover(resetModel.Email, out username))
                                {
                                    response.StatusCode = 500; //Internal Server Error
                                    response.StatusDescription = "Server Error: Couldn't retrieve your account info.";
                                    response.OutputStream.Close();

                                    break;
                                }

                                //Try sending an account recovery mail
                                //4b. Was it successful?
                                if (!client.AccountSendMail(resetModel.Email, username, null))
                                {
                                    response.StatusCode = 500; //Internal Server Error
                                    response.StatusDescription = "Server Error: Failed to send recovery info.";
                                    response.OutputStream.Close();

                                    break;
                                }

                                // Done!
                                RequestResponseData = Encoding.UTF8.GetBytes(resetModel.Email);

                                response.StatusCode = 200; //OK
                                response.ContentLength64 = RequestResponseData.Length;
                                response.OutputStream.Write(RequestResponseData, 0, RequestResponseData.Length);
                                response.OutputStream.Close();
                                break;
                            }

                            //Password Reset. 
                            //Try generating a reset token
                            else
                            {
                                // 3. Are the credentials good?
                                if (resetModel.Username != null && !client.UsernameExists(resetModel.Username))
                                {
                                    response.StatusCode = 404; //Not Found
                                    response.StatusDescription = "Username doesn't exist.";
                                    response.OutputStream.Close();

                                    break;
                                }

                                // 4a. Was it successful?
                                string[] parameters; //Email = 0, Token = 1
                                if (!client.AccountReset(resetModel.Username, out parameters))
                                {
                                    response.StatusCode = 500; //Internal Server Error
                                    response.StatusDescription = "Server error: Reset creation failed.";
                                    response.OutputStream.Close();

                                    break;
                                }

                                //Try sending a password reset link
                                // 4b. Was it successful?
                                if (!client.AccountSendMail(parameters[0], resetModel.Username, parameters[1]))
                                {
                                    response.StatusCode = 500; //Internal Server Error
                                    response.StatusDescription = "Server Error: Failed to send reset link.";
                                    response.OutputStream.Close();

                                    break;
                                }
                                emailResponse = client.EncodeEmail(parameters[0]);
                            }

                            // Done!
                            RequestResponseData = Encoding.UTF8.GetBytes(emailResponse);

                            response.StatusCode = 200; //OK
                            response.ContentLength64 = RequestResponseData.Length;
                            response.OutputStream.Write(RequestResponseData, 0, RequestResponseData.Length);
                            response.OutputStream.Close();
                            break;
                        }

                        //Are we resetting a password?
                        if (request.Url.AbsolutePath.Contains("reset-password"))
                        {
                            string resetData = new StreamReader(request.InputStream).ReadToEnd();
                            var resetModel = JsonConvert.DeserializeObject<Account.ResetRequestModel>(resetData);

                            response.AppendHeader("Access-Control-Allow-Origin", "*");
                            response.AppendHeader("Access-Control-Allow-Methods", "POST, GET");

                            //1. Is the data valid?
                            if (!resetModel.IsRequestValid())
                            {
                                response.StatusCode = 400; //Bad Request
                                byte[] ResetResponseData = Encoding.UTF8.GetBytes("false");

                                response.ContentLength64 = ResetResponseData.Length;
                                response.OutputStream.Write(ResetResponseData, 0, ResetResponseData.Length);
                                response.OutputStream.Close();
                            }

                            //2. Does the token exist?
                            resetModel.Token = HttpUtility.UrlDecode(resetModel.Token);
                            if (!client.IsTokenValid(resetModel.Token))
                            {
                                responseString = Encoding.UTF8.GetBytes("false");
                                response.StatusCode = 404; //Not Found

                                response.ContentLength64 = responseString.Length;
                                response.OutputStream.Write(responseString, 0, responseString.Length);
                                response.OutputStream.Close();

                                break;
                            }

                            //3. Was the reset successful?
                            if(!client.AccountPasswordUpdate(resetModel.Token, resetModel.Password))
                            {
                                responseString = Encoding.UTF8.GetBytes("false");
                                response.StatusCode = 500; //Internal Server Error

                                response.ContentLength64 = responseString.Length;
                                response.OutputStream.Write(responseString, 0, responseString.Length);
                                response.OutputStream.Close();

                                break;
                            }

                            responseString = Encoding.UTF8.GetBytes("true");
                            response.StatusCode = 200; //Ok

                            response.ContentLength64 = responseString.Length;
                            response.OutputStream.Write(responseString, 0, responseString.Length);
                            response.OutputStream.Close();

                            break;
                        }

                        //Infantry Login
                        string loginData = new StreamReader(request.InputStream).ReadToEnd();
                        var loginModel = JsonConvert.DeserializeObject<Account.LoginRequestModel>(loginData);
                        // 2. Is the data valid?
                        if (!loginModel.IsRequestValid())
                        {
                            response.StatusCode = 400; //BadRequest
                            response.OutputStream.Close();

                            break;
                        }

                        // 3a. Are the credentials good?
                        if (!client.UsernameExists(loginModel.Username))
                        {
                            response.StatusCode = 404; //Not Found
                            response.StatusDescription = "Username doesn't exist.";
                            response.OutputStream.Close();

                            break;
                        }

                        // 3b. Are the credentials good?
                        if (!client.IsAccountValid(loginModel.Username, loginModel.PasswordHash))
                        {
                            response.StatusCode = 404; //Not Found
                            response.StatusDescription = "Incorrect Password.";
                            response.OutputStream.Close();

                            break;
                        }

                        // Try logging in
                        Account a = client.AccountLogin(loginModel.Username, loginModel.PasswordHash, request.RemoteEndPoint.Address.ToString());

                        // 4. Was it successful?
                        if (a == null)
                        {
                            response.StatusCode = 400; //BadRequest
                            response.StatusDescription = "Account doesn't exist.";
                            response.OutputStream.Close();

                            break;
                        }

                        var loginResponseModel = new Account.LoginResponseModel();
                        loginResponseModel.Username = a.Username;
                        loginResponseModel.Email = a.Email;
                        loginResponseModel.TicketId = a.SessionId;
                        loginResponseModel.DateCreated = a.DateCreated;
                        loginResponseModel.LastAccessed = a.LastAccessed;
                        loginResponseModel.Permission = a.Permission;

                        var loginResponseString = JsonConvert.SerializeObject(loginResponseModel);
                        byte[] loginResponseData = Encoding.UTF8.GetBytes(loginResponseString);

                        response.StatusCode = 200; //OK
                        response.ContentLength64 = loginResponseData.Length;
                        response.OutputStream.Write(loginResponseData, 0, loginResponseData.Length);
                        response.OutputStream.Close();

                        break;
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("SQL"))
                    Console.WriteLine("Unhandled SQL exception");
            }
        }
    }
}
