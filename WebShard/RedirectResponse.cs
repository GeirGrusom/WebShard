using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    public class RedirectResponse : IResponse
    {
        private readonly string _redirectUrl;

        public string RedirectUrl { get { return _redirectUrl; } }

        public RedirectResponse(string redirectUrl)
        {
            _redirectUrl = redirectUrl;
        }


        public void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            context.Status = new Status(303, "See other");
            context.Headers["Location"] = _redirectUrl;
        }
    }
}
