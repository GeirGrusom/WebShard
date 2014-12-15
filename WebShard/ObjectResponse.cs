using System;
using System.Collections.Generic;
using System.Linq;
using WebShard.Serialization;

namespace WebShard
{
    public class ObjectResponse<T> : IResponse
    {
        private readonly T _value;
        private readonly IResponseSerializer _serializer;

        /// <summary>
        /// Gets the serializer used to serialize <see cref="Value"/> to the stream.
        /// </summary>
        public IResponseSerializer Serializer { get { return _serializer; } }

        /// <summary>
        /// Gets the assigned value to be serialized to the response stream.
        /// </summary>
        public T Value { get { return _value; } }

        public ObjectResponse(T value, IResponseSerializer serializer)
        {
            _value = value;
            _serializer = serializer;
        }

        public ObjectResponse(T value)
            : this(value, null)
        {
            _value = value;
        }

        public virtual void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            context.Headers.ContentType = _serializer.ContentType;
            _serializer.Serialize(_value, context.Response);
        }
    }
}
