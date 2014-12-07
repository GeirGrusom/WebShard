using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    public class ContentResponse : IResponse
    {
        private readonly string _content;

        public string Content { get { return _content; } }

        public ContentResponse(string content)
        {
            _content = content;
        }

        public void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            context.Headers.ContentType = "text/html; charset=UTF-8";
            var data = Encoding.UTF8.GetBytes(_content);
            context.Response.Write(data, 0, data.Length);
            context.Response.Flush();
        }
    }
}
