using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WebShard.Routing;
using WebShard.Serialization;

namespace WebShard.Mvc
{
    public interface IActionInvoker
    {
        IResponse Invoke(object controller, IHttpRequestContext request, IDictionary<string, object> routeValues, IRequestDeserializer deserializer);
        IResponse Invoke(object controller, IHttpRequestContext request, IDictionary<string, object> routeValues);
    }
    public class ActionInvoker : IActionInvoker
    {
        private readonly Type _controllerType;
        private readonly MethodInfo[] _methods;
        private readonly Dictionary<string, MethodInfo[]> _methodLookup;

        public Type ControllerType { get { return _controllerType; } }

        public ActionInvoker(Type controllerType)
        {
            _controllerType = controllerType;
            _methods = _controllerType.GetMethods();
            var methodNames = _methods.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            _methodLookup = methodNames.ToDictionary(x => x, x => 
                _methods.Where(m => string.Equals(x, m.Name, StringComparison.OrdinalIgnoreCase)).ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        public IResponse Invoke(object controller, IHttpRequestContext request, IDictionary<string, object> routeValues)
        {
            return Invoke(controller, request, routeValues, null);
        }

        public IResponse Invoke(object controller, IHttpRequestContext request, IDictionary<string, object> routeValues, IRequestDeserializer deserializer)
        {
            MethodInfo[] methods;
            if (!_methodLookup.TryGetValue((string) routeValues["action"], out methods))
                return StatusResponse.NotFound;

            foreach (var m in methods.OrderByDescending(p => p.GetParameters().Length))
            {
                var parameters = m.GetParameters();
                var notMatchedParameters = parameters.Where(p => !routeValues.ContainsKey(p.Name) && !p.HasDefaultValue).ToArray();
                var matchedParameters = parameters.Where(p => routeValues.ContainsKey(p.Name)).ToDictionary(p => p, p => Convert.ChangeType(routeValues[p.Name], p.ParameterType));
                var defaultParameters = parameters.Where(p => !routeValues.ContainsKey(p.Name) && p.HasDefaultValue).ToDictionary(p => p, p => p.DefaultValue);

                if (!notMatchedParameters.Any())
                {
                    object result = m.Invoke(controller,
                        matchedParameters.Concat(defaultParameters)
                            .OrderBy(par => par.Key.Position)
                            .Select(par => par.Value)
                            .ToArray());
                    return (IResponse) result;
                }

                if (deserializer != null)
                {
                    if (notMatchedParameters.Length == 1)
                    {
                        var p = notMatchedParameters[0];

                        object value = deserializer.Deserialize(request.Body, p.ParameterType);
                            
                        var values = new Dictionary<ParameterInfo, object>
                            {{ p, value }};
                        var invokeParameters = matchedParameters.Concat(defaultParameters).Concat(values).OrderBy(par => par.Key.Position).Select(par => par.Value).ToArray();

                        object result = m.Invoke(controller, invokeParameters);
                        return (IResponse)result;
                    }
                }
                if (notMatchedParameters.Length == 0)
                {
                    object result = m.Invoke(controller, matchedParameters.Concat(defaultParameters).OrderBy(par => par.Key.Position).Select(par => par.Value).ToArray());
                    return (IResponse)result;
                }
            }
            return StatusResponse.NotFound;
        }
    }
}
