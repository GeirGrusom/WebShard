using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WebShard
{
    public class HttpRequestContext : IHttpRequestContext
    {
        private readonly string _protocolVersion;
        private readonly string _method;
        private readonly Uri _uri;
        private readonly string _remoteAddress;
        private readonly HeaderCollection _headerCollection;
        private readonly Stream _bodyStream;

        public string ProtocolVersion { get { return _protocolVersion; } }
        public string Method { get { return _method; } }
        public Uri Uri {  get { return _uri; }}
        public string RemoteAddress { get { return _remoteAddress; } }
        public HeaderCollection Headers { get { return _headerCollection; } }
        public Stream Body { get { return _bodyStream; } }

        private HttpRequestContext(string method, Uri uri, string protocolVersion, string remoteAddress, HeaderCollection headers,
            Stream body)
        {
            _protocolVersion = protocolVersion;
            _uri = uri;
            _method = method;
            _remoteAddress = remoteAddress;
            _headerCollection = headers;
            _bodyStream = body;
        }
        
        private static readonly Regex headerRegex = new Regex(@"(?<Name>[a-zA-Z\-]+)\s*:\s*(?<Value>.+)", RegexOptions.Compiled | RegexOptions.Singleline);
        
        public static HttpRequestContext CreateFromStream(Stream source, string scheme, string remoteAddress)
        {
            using (var reader = new StreamReader(source, Encoding.UTF8, false, bufferSize: 8192, leaveOpen: true))
            {
                var topHeader = reader.ReadLine();
                if(topHeader == null)
                    throw new EndOfStreamException();
                var fields = topHeader.Split(' ');
                string line;
                var headers = new HeaderCollection();
                while ("" != (line = reader.ReadLine()))
                {
                    if (line == null)
                        throw new EndOfStreamException();

                    var match = headerRegex.Match(line);
                    if(match.Success)
                        headers.Add(match.Groups["Name"].Value, match.Groups["Value"].Value);
                }
                return new HttpRequestContext(fields[0], new Uri(scheme + "://" + headers.Host + fields[1]), fields[2], remoteAddress, headers, source);
            }
        }
    }
}