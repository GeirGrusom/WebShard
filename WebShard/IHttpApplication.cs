using System;
using System.Collections.Generic;
using WebShard.Routing;
using WebShard.Serialization;

namespace WebShard
{
    using Ioc;


    public sealed class ApplicationExceptionEventArgs : EventArgs
    {
        private readonly Exception _exception;
        private readonly bool _processesByExceptionFilter;
        private IResponse _response;
        private readonly IHttpRequestContext _request;

        public void SetResponse(IResponse response)
        {
            _response = response;
        }

        public Exception Exception { get { return _exception; } }
        internal IResponse Response { get { return _response; } }
        public IHttpRequestContext Request { get { return _request; } }

        public ApplicationExceptionEventArgs(Exception exception, IHttpRequestContext request, bool processedByExceptionFilter)
        {
            _exception = exception;
            _request = request;
            _processesByExceptionFilter = processedByExceptionFilter;
        }
    }

    public delegate void ApplicationException(IHttpApplication application, ApplicationExceptionEventArgs ev);

    public interface IHttpApplication
    {
        event ApplicationException ApplicationException;
        IContainer Container { get; }
        IRouteTable RouteTable { get; }
        IHttpResponseContext ProcessRequest(IHttpRequestContext requestContext);
    }
}
