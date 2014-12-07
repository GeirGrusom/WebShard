using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    [Serializable]
    public class HttpException :  Exception
    {
        private readonly int _statusCode;
        public int StatusCode { get { return _statusCode; }  }

        internal HttpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _statusCode = info.GetInt32("_statusCode");
        }

        public HttpException(int statusCode, string message)
            : base(message)
        {
            _statusCode = statusCode;
        }

        public HttpException(Status status)
            : this(status.Code, status.Description)
        {
        }

        public HttpException(int statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            _statusCode = statusCode;
        }

        public HttpException(Status status, Exception innerException)
            : this(status.Code, status.Description, innerException)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_statusCode", _statusCode);
        }
    }

    [Serializable]
    public sealed class BadRequestException : HttpException
    {
        public BadRequestException()
            : base(Status.BadRequest)
        {
        }
    }

    [Serializable]
    public sealed class NotFoundException : HttpException
    {
        public NotFoundException()
            : base(Status.NotFound)
        {
            
        }
    }

    [Serializable]
    public sealed class InternalServerError : HttpException
    {
        public InternalServerError()
            : base(Status.InternalServerError)
        {
            
        }
    }

    [Serializable]
    public sealed class UnauthorizedException : HttpException
    {
        public UnauthorizedException()
            : base(Status.Unauthorized)
        {
        }
    }
}
