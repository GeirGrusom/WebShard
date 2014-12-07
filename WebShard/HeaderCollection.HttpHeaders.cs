using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    public partial class HeaderCollection
    {
        private const string HttpHost = "Host";
        private const string HttpAccept = "Accept";
        private const string HttpAcceptCharset = "Accept-Charset";
        private const string HttpAcceptEncoding = "Accept-Encoding";
        private const string HttpAuthorization = "Authorization";
        private const string HttpContentLength = "Content-Length";
        private const string HttpContentType = "Content-Type";
        private const string HttpETag = "ETag";
        private const string HttpIfNoneMatch = "If-None-Match";
        private const string HttpUserAgent = "User-Agent";
        private const string HttpConnection = "Connection";
        private const string HttpContentEncoding = "Content-Encoding";
        private const string HttpCookie = "Cookie";
        
        public string Host { get { return this[HttpHost]; } set { this[HttpHost] = value; } }

        public string Accept { get { return this[HttpAccept]; } set { this[HttpAccept] = value; } }
        public string AcceptCharset { get { return this[HttpAcceptCharset]; } set { this[HttpAcceptCharset] = value; } }
        public string[] AcceptEncoding { get { return (this[HttpAcceptEncoding] ?? "").Split(',').Select(s => s.Trim()).ToArray(); } set
        {
            this[HttpAcceptEncoding] = string.Join(", ", value);
        } }
        public string Authorization { get { return this[HttpAuthorization]; } set { this[HttpAuthorization] = value; } }
        public string Connection { get { return this[HttpConnection]; } set { this[HttpConnection] = value; } }
        public string ContentEncoding { get { return this[HttpContentEncoding]; } set { this[HttpContentEncoding] = value; } }

        public long? ContentLength
        {
            get
            {
                long result;
                if (this[HttpContentLength] != null && long.TryParse(this[HttpContentLength], out result))
                {
                    if (result < 0)
                        throw new BadRequestException();
                    return result;
                }

                return null;
            }
            set
            {
                if (value == null)
                    Remove(HttpContentLength);
                this[HttpContentLength] = value.ToString();
            }
        }
        public string ContentType { get { return this[HttpContentType]; } set { this[HttpContentType] = value; } }
        public string Cookie { get { return this[HttpCookie]; } set { this[HttpCookie] = value; } }
        public string ETag { get { return this[HttpETag]; } set { this[HttpETag] = value; } }
        public string IfNoneMatch { get { return this[HttpIfNoneMatch]; } set { this[HttpIfNoneMatch] = value; } }
        public string UserAgent { get { return this[HttpUserAgent]; } set { this[HttpUserAgent] = value; } }

    }
}
