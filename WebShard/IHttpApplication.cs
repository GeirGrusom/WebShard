using System;
using WebShard.Routing;

namespace WebShard
{
    using Ioc;
    public interface IHttpApplication
    {
        IContainer Container { get; }
        IRouteTable RouteTable { get; }
        IHttpResponseContext ProcessRequest(IHttpRequestContext requestContext);
    }
}
