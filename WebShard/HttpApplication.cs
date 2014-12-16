using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebShard.Filtering;
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

        public event ApplicationException ApplicationException;
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

        protected virtual void OnApplicationException(ApplicationExceptionEventArgs ev)
        {
            var @event = ApplicationException;
            if (@event != null)
                @event(this, ev);
        }

        private IResponse ErrorResponse(Exception ex, IHttpRequestContext request)
        {
            IResponse statusCode;
            var exception = ex as HttpException;
            if (exception != null)
                statusCode = new StatusResponse(new Status(exception.StatusCode, exception.Message));
            else
                statusCode = StatusResponse.InternalServerError;

            return statusCode;
        }

        public IHttpResponseContext ProcessRequest(IHttpRequestContext requestContext)
        {
            IHttpResponseContext response;
            using (var requestContainer = _container.CreateRequestChildContainer())
            {
                requestContainer.For<IHttpRequestContext>().Use(c => requestContext, Lifetime.Application);
                var filterRegistry = FilterRegistry.CreateProxyContainer(requestContainer);
                try
                {
                    response = InternalProcessRequest(requestContext, requestContainer);

                    var responseFilter = filterRegistry.TryGet<IResponseFilter>();
                    if (responseFilter != null)
                        responseFilter.Process(requestContext, response);

                }
                catch (HttpException ex)
                {
                    var responseFilter = filterRegistry.TryGet<IExceptionFilter>();
                    response = new HttpResponseContext(requestContext);
                    try
                    {
                        if (responseFilter != null)
                            responseFilter.Process(requestContext, response, ex);

                        var exArgs = new ApplicationExceptionEventArgs(ex, requestContext, responseFilter != null);
                        OnApplicationException(exArgs);
                        if (exArgs.Response != null)
                        {
                            response = new HttpResponseContext(requestContext);
                            exArgs.Response.Write(requestContext, response);
                        }
                        else if (responseFilter == null)
                        {
                            response = new HttpResponseContext(requestContext);
                            ErrorResponse(ex, requestContext).Write(requestContext, response);
                        }
                    }
                    catch (Exception exception)
                    {
                        var newArgs = new ApplicationExceptionEventArgs(exception, requestContext, false);
                        OnApplicationException(newArgs);
                        if (newArgs.Response != null)
                        {
                            response = new HttpResponseContext(requestContext);
                            newArgs.Response.Write(requestContext, response);
                        }
                        else
                        {
                            response = new HttpResponseContext(requestContext);
                            ErrorResponse(ex, requestContext).Write(requestContext, response);
                        }
                    }
                }
            }
            return response;
        }

        public IHttpResponseContext InternalProcessRequest(IHttpRequestContext requestContext, IContainer requestContainer)
        {
            var response = new HttpResponseContext(requestContext);

            IDictionary<string, object> routeValues;
            var route = _routeTable.Match(SanitizePathAndQueryAndReturnPath(requestContext.Uri.PathAndQuery),
                out routeValues);

            var keepAlive = requestContext.Headers.Connection == "keep-alive";

            response.Headers.Connection = keepAlive ? "keep-alive" : "close";

            if (route == null) // No route was matched.
            {
                StatusResponse.NotFound.Write(requestContext, response);
                return response;
            }


            object controller;
            object controllerType;
            if (routeValues.TryGetValue("controller", out controllerType))
            {
                var proxyControllerContainer = _controllerRegistry.CreateProxyContainer(requestContainer);
                if (controllerType is string || controllerType is Type)
                {
                    if (controllerType is string)
                        controller = proxyControllerContainer.TryGet((string)controllerType + "Controller");
                    else
                        controller = proxyControllerContainer.GetService((Type)controllerType);
                }
                else if (controllerType.GetType().IsGenericType &&
                         controllerType.GetType().GetGenericTypeDefinition() == typeof(Func<>))
                {
                    try
                    {
                        controller = ((Func<object>) controllerType)();
                    }
                    catch (HttpException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new HttpException(Status.InternalServerError, ex);
                    }
                }
                else if (controllerType.GetType().IsGenericType &&
                         controllerType.GetType().GetGenericTypeDefinition() == typeof(Func<,>))
                {
                    try
                    {
                        controller = ((Func<IHttpRequestContext, object>) controllerType)(requestContext);
                    }
                    catch (HttpException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new HttpException(Status.InternalServerError, ex);
                    }
                }
                else
                    controller = null;
            }
            else
                controller = null;

            // Not found
            if (controller == null && routeValues.ContainsKey("action") && routeValues["action"] == null)
            {
                StatusResponse.NotFound.Write(requestContext, response);
                return response;
            }

            if (!routeValues.ContainsKey("action"))
                routeValues["action"] = requestContext.Method;


            var filterRegistryProxy = _filterRegistry.CreateProxyContainer(requestContainer);
            var filters = filterRegistryProxy.GetAll<IRequestFilter>(recurse: false);

            IResponse result;
            try
            {
                IResponse filterResponse = filters.Select(f => f.Process()).FirstOrDefault(r => r != null);
                if (filterResponse != null)
                {
                    filterResponse.Write(requestContext, response);
                    requestContainer.Dispose();
                    return response;
                }
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpException(Status.InternalServerError, ex);
            }



            object action;
            if (!routeValues.TryGetValue("action", out action) && controller == null)
            {
                result = StatusResponse.NotFound;
            }
            else
            {
                if (action is string && controller != null)
                {
                    try
                    {
                        result = InvokeAction(requestContext, controller, routeValues);
                    }
                    catch (HttpException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new HttpException(Status.InternalServerError, ex);
                    }
                }
                else
                {
                    try
                    {
                        if (action is Func<IResponse>)
                            result = ((Func<IResponse>) action)();
                        else if (action is Func<IHttpRequestContext, IResponse>)
                            result = ((Func<IHttpRequestContext, IResponse>) action)(requestContext);
                        else
                            throw new NotSupportedException(string.Format("The type '{0}' is not supprted as a action.",
                                action.GetType()));
                    }
                    catch (HttpException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new HttpException(Status.InternalServerError, ex);
                    }
                }
            }

            if (result != null)
            {
                try
                {
                    result.Write(requestContext, response);
                }
                catch (HttpException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new HttpException(Status.InternalServerError, ex);
                }
            }

            else
            {
                StatusResponse.NotFound.Write(requestContext, response);
            }
            return response;
        }

        private IResponse InvokeAction(IHttpRequestContext requestContext, object controller, IDictionary<string, object> routeValues)
        {
            IResponse result;
            var actionInvoker = new ActionInvoker(controller.GetType());

            try
            {
                IRequestDeserializer deserializer;
                if (requestContext.Headers.ContentType != null &&
                    _deserializers.TryGetValue(requestContext.Headers.ContentType, out deserializer))
                    result = actionInvoker.Invoke(controller, requestContext, routeValues, deserializer);
                else
                    result = actionInvoker.Invoke(controller, requestContext, routeValues);
            }
            catch (JsonDeserializationException ex)
            {
                throw new HttpException(Status.BadRequest, ex);
            }
            catch (Exception ex)
            {
                throw new HttpException(Status.InternalServerError, ex);
            }
            return result;
        }
    }
}