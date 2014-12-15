using System;
using System.Collections.Generic;
using WebShard.Routing;
using WebShard.Serialization;

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
