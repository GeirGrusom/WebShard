using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Filtering
{
    public class BasicAuthorizationFilter : IRequestFilter
    {
        private readonly IHttpRequestContext _requestContext;
        private readonly string _username;
        private readonly string _password;

        public BasicAuthorizationFilter(IHttpRequestContext requestContext, string username, string password)
        {
            _requestContext = requestContext;
            _username = username;
            _password = password;
        }

        public IResponse Process()
        {
            string auth = _requestContext.Headers.Authorization;
            if (string.IsNullOrEmpty(auth))
               return StatusResponse.Unauthorized;

            return null;
        }
    }
}
