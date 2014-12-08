using System;
using System.Collections.Generic;
using System.Linq;
using WebShard.Mvc;

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

        public event EventHandler<IContainer> ConfigureRequest; 

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
            var requestContainer = _container.CreateChildContainer();

            requestContainer.For<IHttpRequestContext>().Use(() => requestContext, Lifetime.Application);

            if (ConfigureRequest != null)
                ConfigureRequest(this, requestContainer);

            IDictionary<string, string> routeValues;
            var route = _routeTable.Match(SanitizePathAndQueryAndReturnPath(requestContext.Uri.PathAndQuery), out routeValues);

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
                result = actionInvoker.Invoke(controller, routeValues);
            }
            catch (HttpException ex)
            {
                var statusResponse = new StatusResponse(new Status(ex.StatusCode, ex.Message));
                statusResponse.Write(requestContext, response);
                return response;
            }
            if (result != null)
                result.Write(requestContext, response);

            requestContainer.Dispose();
            return response;
        }
    }
}