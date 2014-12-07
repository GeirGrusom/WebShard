using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    public class HttpResponseContext : IHttpResponseContext
    {
        private readonly IHttpRequestContext _httpRequestContext;
        private readonly HeaderCollection _headers;
        private readonly MemoryStream _contentStream;
        private readonly Stream _stream;

        public HeaderCollection Headers { get { return _headers; } }
        public Status Status { get; set; }
        public Stream Response { get { return _stream; } }

        public HttpResponseContext(IHttpRequestContext httpRequestContext)
        {
            _httpRequestContext = httpRequestContext;
            _headers = new HeaderCollection();
            _contentStream = new MemoryStream();

            if (_httpRequestContext.Headers.AcceptEncoding.Contains("gzip"))
            {
                _headers.ContentEncoding = "gzip";
                _stream = new GZipStream(_contentStream, CompressionLevel.Fastest);
            }
            else if (_httpRequestContext.Headers.AcceptEncoding.Contains("deflate"))
            {
                _headers.ContentEncoding = "deflate";
                _stream = new DeflateStream(_contentStream, CompressionLevel.Fastest);
            }
            else
                _stream = _contentStream;

            Status = Status.Ok;
        }

        private readonly string[] days =  { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private readonly string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public void WriteResponse(Stream dst)
        {
            _stream.Dispose();
            var message = _contentStream.ToArray();
            _headers.ContentLength = message.Length;
            var now = DateTimeOffset.Now;
            _headers["Date"] = string.Concat(
                days[(int)now.DayOfWeek], ", ",
                now.Day, " ", 
                months[now.Month - 1], " ",
                now.Year, " ",
                now.Hour,
                ":",
                now.Minute,
                ":",
                now.Second, " ",
                now.Offset.Hours == 0 ? "GMT" : ("GMT+" + now.Offset.Hours));
            var headerBuilder = new StringBuilder();

            headerBuilder.AppendFormat("HTTP/1.1 {0}\r\n", Status);
            foreach (var header in _headers)
            {
                headerBuilder.AppendFormat("{0}\r\n", header);
            }
            headerBuilder.Append("\r\n");
            var headerBytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
            dst.Write(headerBytes, 0, headerBytes.Length);
            dst.Write(message, 0, message.Length);
        }
    }
}
