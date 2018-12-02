using System;

namespace Infantry_Launcher.Protocol.Helpers
{
    public class IStatus
    {
        public class LoginRequestObject
        {
            public string Username;
            public string PasswordHash;
        }

        public class RegisterRequestObject
        {
            public string Username;
            public string PasswordHash;
            public string Email;
        }

        public class RecoverRequestObject
        {
            public string Username;
            public string Email;
            public bool Reset;
        }

        public class LoginResponseObject
        {
            public string Username;
            public string Email;
            public Guid TicketId;
            public DateTime DateCreated;
            public DateTime LastAccessed;
            public int Permission;
        }

        public enum PingRequestStatusCode
        {
            Ok,
            NotFound,
        }

        public enum RegistrationStatusCode
        {
            Ok,
            MalformedData,
            UsernameTaken,
            EmailTaken,
            WeakCredentials,
            ServerError,
            NoResponse,
        }

        public enum LoginStatusCode
        {
            Ok,
            MalformedData,
            InvalidCredentials,
            ServerError,
            NoResponse,
        }

        public enum RecoverStatusCode
        {
            Ok,
            MalformedData,
            InvalidCredentials,
            ServerError,
            NoResponse,
        }
    }
}