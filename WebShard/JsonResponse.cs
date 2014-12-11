using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WebShard.Serialization;

namespace WebShard
{
    public sealed class JsonResponse : IResponse
    {
        private readonly object _model;
        private readonly ISerializer _serializer;
        public JsonResponse(object model)
            : this(model, JsonSerializer.Instance)
        {
        }

        public JsonResponse(object model, ISerializer serializer)
        {
            _model = model;
            _serializer = serializer;
        }

        public object Model { get { return _model; }}
        public void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            string text = _serializer.Serialize(_model);
            using (var streamWriter = new StreamWriter(context.Response, Encoding.UTF8))
            {
                streamWriter.Write(text);
            }
            context.Headers.ContentType = "application/json";
        }
    }
}
