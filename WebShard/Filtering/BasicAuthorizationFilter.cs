using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Filtering
{

    public class PasswordCredentials
    {
        private readonly string _username;
        private readonly string _password;

        public string Username { get { return _username; } }
        public string Password { get { return _password; } }

        public PasswordCredentials(string username, string password)
        {
            _username = username;
            _password = password;
        }
    }

    public class UnauthorizedResponse : IResponse
    {
        public static readonly UnauthorizedResponse Instance = new UnauthorizedResponse();

        public void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            context.Headers["WWW-Authenticate"] = "Basic";
            context.Status = Status.Unauthorized;
        }
    }

    public class BasicAuthorizationFilter : IRequestFilter
    {
        private readonly IHttpRequestContext _requestContext;
        private readonly PasswordCredentials _credentials;

        public BasicAuthorizationFilter(IHttpRequestContext requestContext, PasswordCredentials credentials)
        {
            _requestContext = requestContext;
            _credentials = credentials;
        }

        public IResponse Process()
        {
            string auth = _requestContext.Headers.Authorization;
            if (string.IsNullOrEmpty(auth))
                return UnauthorizedResponse.Instance;

            string[] values = auth.Split(' ');
            if (values.Length != 2)
                return UnauthorizedResponse.Instance;

            if (string.Equals(values[0], "Basic", StringComparison.OrdinalIgnoreCase))
            {
                string value = Encoding.UTF8.GetString(Convert.FromBase64String(values[1]));
                var index = value.IndexOf(':');
                if (index < 0)
                    return UnauthorizedResponse.Instance;
                string username = value.Substring(0, index);
                string password = value.Substring(index + 1);

                if (string.Equals(username, _credentials.Username, StringComparison.OrdinalIgnoreCase)
                    && password == _credentials.Password)
                    return null;

                return UnauthorizedResponse.Instance;
            }

            return null;
        }
    }
}
