using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
