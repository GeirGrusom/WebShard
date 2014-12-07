using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            var methods = _methodLookup[routeValues["action"]];
            var method =
                methods.Where(x => x.GetParameters().All(p => routeValues.ContainsKey(p.Name)))
                    .OrderByDescending(x => x.GetParameters().Length)
                    .FirstOrDefault();
            if (method == null)
            {
                return null;
            }
            var parameters = method.GetParameters();
            var result = (IResponse) method.Invoke(controller, parameters.Select(p => Convert.ChangeType(routeValues[p.Name], p.ParameterType)).ToArray());
            return result;
        }
    }
}
