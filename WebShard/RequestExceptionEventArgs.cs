using System;

namespace WebShard
{
    public sealed class RequestExceptionEventArgs
    {
        private readonly IHttpRequestContext _requestContext;
        private readonly Exception _exception;
        public IHttpRequestContext RequestContext { get { return _requestContext; } }
        public Exception Exception { get { return _exception; } }

        public RequestExceptionEventArgs(IHttpRequestContext requestContext, Exception exception)
        {
            _exception = exception;
            _requestContext = requestContext;
        }
    }
}