using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Routing
{
    using Ioc;
    public class Route
    {
        private readonly IContainer _container;
        private readonly string _url;
        private readonly object _defaults;
        private readonly RouteMatcher _matcher;

        public string Url { get { return _url; } }
        public object Defaults { get { return _defaults; } }

        public Route(IContainer controllerContainer, string url, object defaults)
        {
            _url = url;
            _defaults = defaults;
            _container = controllerContainer;
            _matcher = new RouteMatcher(this);
        }

        public bool Match(string url, out IDictionary<string, object> routeValues)
        {
            return _matcher.Match(url, out routeValues);
        }

        public IContainer ControllerContainer { get { return _container; } }
    }
}
