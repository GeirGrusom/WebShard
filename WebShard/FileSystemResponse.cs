using System;
using System.IO;
using System.Security;

namespace WebShard
{
    public class FileSystemResponse : IResponse
    {
        private readonly string _filename;
        private readonly string _mimeType;
        private readonly string _encoding;

        public string Filename { get { return _filename; } }
        public string Mimetype { get { return _mimeType; } }
        public string Encoding { get { return _encoding; } }

        public FileSystemResponse(string filename, string mimeType, string encoding)
        {
            _filename = filename;
            _mimeType = mimeType;
            _encoding = encoding;
        }

        public void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            if (!File.Exists(_filename))
            {
                StatusResponse.NotFound.Write(request, context);
                return;
            }
            string etag = request.Headers.IfNoneMatch;
            var fi = new FileInfo(_filename);
            string hashCode = fi.LastWriteTime.GetHashCode().ToString("x8");
            if (hashCode == etag)
            {
                context.Status = Status.NotModified;
                return;
            }

            try
            {
                using (var fs = new FileStream(_filename, FileMode.Open, FileAccess.Read))
                {
                    fs.CopyTo(context.Response);
                }
                if (_encoding != null)
                    context.Headers.ContentType = _mimeType + "; charset=" + _encoding;
                else
                    context.Headers.ContentType = _mimeType;
                context.Headers.ETag = hashCode;
            }
            catch (DirectoryNotFoundException)
            {
                context.Status = Status.NotFound;
            }
            catch (FileNotFoundException)
            {
                context.Status = Status.NotFound;
            }
            catch (UnauthorizedAccessException)
            {
                context.Status = Status.Forbidden;
            }
            catch (SecurityException)
            {
                context.Status = Status.Forbidden;
            }
        }
    }
}
