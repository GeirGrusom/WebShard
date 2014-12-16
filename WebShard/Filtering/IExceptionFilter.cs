using System;

namespace WebShard.Filtering
{
    public interface IResponseFilter
    {
        IResponse Process(IHttpRequestContext request, IHttpResponseContext response);
    }

    public interface IExceptionFilter
    {
        IResponse Process(IHttpRequestContext request, IHttpResponseContext response, Exception ex);
    }
}
