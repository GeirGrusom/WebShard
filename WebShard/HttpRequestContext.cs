using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
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
            _queryString = Routing.QueryString.Parse(uri.Query);
        }
        
        private static readonly Regex headerRegex = new Regex(@"(?<Name>[a-zA-Z\-]+)\s*:\s*(?<Value>.+)", RegexOptions.Compiled | RegexOptions.Singleline);

        internal class NullStream : Stream
        {

            public static readonly NullStream Instance = new NullStream();

            public override void Flush()
            {
                
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Length
            {
                get { return 0; }
            }

            public override long Position { get { return 0; }  set {throw new NotSupportedException();}}
        }

        private static string ReadLine(Stream source)
        {
            var bytes = new List<byte>();
            int value;
            while(13 != (value = source.ReadByte()))
                bytes.Add((byte)value);

            source.ReadByte();
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static HttpRequestContext CreateFromStream(Stream source, string scheme, string remoteAddress)
        {
            var stream = new BufferedStream(source, 4096);
            {
                var topHeader = ReadLine(stream);
                if (topHeader == null)
                    throw new EndOfStreamException();
                var fields = topHeader.Split(' ');
                string line;
                var headers = new HeaderCollection();
                while ("" != (line = ReadLine(stream)))
                {
                    if (line == null)
                        throw new EndOfStreamException();

                    var match = headerRegex.Match(line);
                    if (match.Success)
                        headers.Add(match.Groups["Name"].Value, match.Groups["Value"].Value);
                }
                Stream contentStream;
                var contentLength = (int) headers.ContentLength.GetValueOrDefault();

                if (contentLength != 0)
                {
                    var readBuffer = new byte[contentLength];
                    stream.Read(readBuffer, 0, readBuffer.Length);
                    contentStream = new MemoryStream(readBuffer, writable: false);
                }
                else
                    contentStream = NullStream.Instance;

                return new HttpRequestContext(fields[0], new Uri(scheme + "://" + headers.Host + fields[1]), fields[2],
                    remoteAddress, headers, contentStream);
            }
        }
    }
}