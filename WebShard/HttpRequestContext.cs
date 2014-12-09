using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebShard
{
    public class HttpRequestContext : IHttpRequestContext
    {
        private static readonly Regex KeyValueRegex = new Regex("(?<Key>[^&=]+)=(?<Value>[^&=]*)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static IDictionary<string, string> ParseQueryString(string queryString)
        {
            var m = KeyValueRegex.Matches(queryString.TrimStart('?')).Cast<Match>();
            return m.ToDictionary(k => System.Net.WebUtility.UrlDecode(k.Groups["Key"].Value), k => System.Net.WebUtility.UrlDecode(k.Groups["Value"].Value));
        }
        private readonly string _protocolVersion;
        private readonly string _method;
        private readonly Uri _uri;
        private readonly string _remoteAddress;
        private readonly HeaderCollection _headerCollection;
        private readonly Stream _bodyStream;
        private readonly IDictionary<string, string> _queryString; 

        public string ProtocolVersion { get { return _protocolVersion; } }
        public string Method { get { return _method; } }
        public Uri Uri {  get { return _uri; }}
        public string RemoteAddress { get { return _remoteAddress; } }
        public HeaderCollection Headers { get { return _headerCollection; } }
        public Stream Body { get { return _bodyStream; } }
        public IDictionary<string, string> QueryString { get { return _queryString; } } 

        private HttpRequestContext(string method, Uri uri, string protocolVersion, string remoteAddress, HeaderCollection headers,
            Stream body)
        {
            _protocolVersion = protocolVersion;
            _uri = uri;
            _method = method;
            _remoteAddress = remoteAddress;
            _headerCollection = headers;
            _bodyStream = body;
            _queryString = ParseQueryString(uri.Query);
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