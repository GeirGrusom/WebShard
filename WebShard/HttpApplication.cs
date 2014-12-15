using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebShard.Mvc;
using WebShard.Serialization;
using WebShard.Serialization.Form;
using WebShard.Serialization.Json;

namespace WebShard
{
    using Ioc;
    using Routing;
    public class HttpApplication : IHttpApplication
    {
        private readonly Container _container;
        private readonly RouteTable _routeTable;
        private readonly IContainer _controllerRegistry;
        private readonly IContainer _filterRegistry;
        private readonly IDictionary<string, IRequestDeserializer> _deserializers; 

        public IDictionary<string, IRequestDeserializer> Deserializers { get { return _deserializers; } }
        public IContainer Container { get { return _container; } }
        public IRouteTable RouteTable { get { return _routeTable; } }
        public IContainer ControllerRegistry { get { return _controllerRegistry; } }
        public IContainer FilterRegistry { get { return _filterRegistry; } }

        public HttpApplication()
        {
            _container = new Container();
            _controllerRegistry = _container.CreateChildContainer();
            _routeTable = new RouteTable(_controllerRegistry);
            _filterRegistry = _container.CreateChildContainer();
            _deserializers = new Dictionary<string, IRequestDeserializer>
            {
                {"application/json", new JsonRequestDeserializer()},
                {"application/x-www-form-urlencoded", new FormRequestDeserializer()}
            };
        }


        private static string SanitizePathAndQueryAndReturnPath(string input)
        {
            int queryStart = input.IndexOf('?');
            if (queryStart > 0)
                input = input.Substring(0, queryStart);

            if (!input.EndsWith("/"))
                return input + "/";

            return input;
        }

        public IHttpResponseContext ProcessRequest(IHttpRequestContext requestContext)
        {
            var response = new HttpResponseContext(requestContext);
            using (var requestContainer = _container.CreateRequestChildContainer())
            {

                requestContainer.For<IHttpRequestContext>().Use(c => requestContext, Lifetime.Application);
                requestContainer.For<IHttpResponseContext>().Use(c => response, Lifetime.Application);

                IDictionary<string, string> routeValues;
                var route = _routeTable.Match(SanitizePathAndQueryAndReturnPath(requestContext.Uri.PathAndQuery),
                    out routeValues);

                var keepAlive = requestContext.Headers.Connection == "keep-alive";

                response.Headers.Connection = keepAlive ? "keep-alive" : "close";

                if (route == null) // No route was matched.
                {
                    StatusResponse.NotFound.Write(requestContext, response);
                    return response;
                }

                if (!routeValues.ContainsKey("action"))
                    routeValues["action"] = requestContext.Method;

                string controllerName = routeValues["controller"];
                var proxyControllerContainer = _controllerRegistry.CreateProxyContainer(requestContainer);

                var controller = proxyControllerContainer.TryGet(controllerName + "Controller");

                // Not found
                if (controller == null)
                {
                    StatusResponse.NotFound.Write(requestContext, response);
                    return response;
                }

                var filterRegistryProxy = _filterRegistry.CreateProxyContainer(requestContainer);
                var filters = filterRegistryProxy.GetAll<IRequestFilter>(recurse: false);

                IResponse filterResponse = filters.Select(f => f.Process()).FirstOrDefault(r => r != null);

                if (filterResponse != null)
                {
                    filterResponse.Write(requestContext, response);
                    requestContainer.Dispose();
                    return response;
                }

                var actionInvoker = new ActionInvoker(controller.GetType());
                IResponse result;
                try
                {
                    IRequestDeserializer deserializer;
                    if (requestContext.Headers.ContentType != null && _deserializers.TryGetValue(requestContext.Headers.ContentType, out deserializer))
                        result = actionInvoker.Invoke(controller, requestContext, routeValues, deserializer);
                    else
                        result = actionInvoker.Invoke(controller, requestContext, routeValues);
                }
                catch (HttpException ex)
                {
                    var statusResponse = new StatusResponse(new Status(ex.StatusCode, ex.Message));
                    statusResponse.Write(requestContext, response);
                    return response;
                }
                if (result != null)
                    result.Write(requestContext, response);

            }
            return response;
        }
    }
}