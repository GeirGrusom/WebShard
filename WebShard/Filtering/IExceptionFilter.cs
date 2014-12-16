using System;

namespace WebShard.Filtering
{
    interface IResponseFilter
    {
        IResponse Process(IHttpRequestContext request, IHttpResponseContext response);
    }

    interface IExceptionFilter
    {
        IResponse Process(IHttpRequestContext request, IHttpResponseContext response, Exception ex);
    }
}
