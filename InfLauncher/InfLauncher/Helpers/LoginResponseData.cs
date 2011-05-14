using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfLauncher.Helpers
{
    /// <summary>
    /// Contains valid data when an account is successfully logged in.
    /// </summary>
    public class LoginResponseData
    {
        /// <summary>
        /// Unique session id associated with this account.
        /// </summary>
        public Guid SessionId;
    }
}
