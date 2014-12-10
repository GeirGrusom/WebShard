using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WebShard.Routing;

namespace WebShard.Mvc
{
    public interface IActionInvoker
    {
        IResponse Invoke(object controller, IDictionary<string, string> routeValues);
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

        public IResponse Invoke(object controller, IDictionary<string, string> routeValues)
        {
            return Invoke(controller, routeValues, null);
        }

        public IResponse Invoke(object controller, IDictionary<string, string> routeValues, string postContent)
        {
            var methods = _methodLookup[routeValues["action"]];
            /*var method =
                methods.Where(x => x.GetParameters().All(p => routeValues.ContainsKey(p.Name) || p.HasDefaultValue))
                    .OrderByDescending(x => x.GetParameters().Length)
                    .FirstOrDefault();
            if (method == null)
            {
                return null;
            }
            var parameters = method.GetParameters();
            var result = (IResponse) method.Invoke(controller, 
                parameters.Select(p => routeValues.ContainsKey(p.Name) ? Convert.ChangeType(routeValues[p.Name], p.ParameterType) : p.DefaultValue).ToArray());
            return result;*/

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

                if (!string.IsNullOrEmpty(postContent))
                {
                    if (notMatchedParameters.Length == 1)
                    {
                        var p = notMatchedParameters[0];
                        object value;
                        if (!QueryString.TryDeserialize(postContent, p.ParameterType, out value))
                            continue;
                        var values = new Dictionary<ParameterInfo, object>
                            {{ p, value }};
                        var invokeParameters = matchedParameters.Concat(defaultParameters).Concat(values).OrderBy(par => par.Key.Position).Select(par => par.Value).ToArray();

                        object result = m.Invoke(controller, invokeParameters);
                        return (IResponse)result;
                    }
                }
            }
            throw new NotImplementedException();
        }
    }
}
