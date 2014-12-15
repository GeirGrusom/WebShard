using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace WebShard.Routing
{
    using Ioc;
    public interface IRouteTable
    {
        IReadOnlyList<Route> Routes { get; }
        void Add(string url, object defaults);
        void Add(Route route);
        Route Match(string url, out IDictionary<string, object> routeValues);
    }

    public class RouteTable : IRouteTable
    {
        private readonly IList<Route> _routes;
        private readonly IContainer _container;

        public IReadOnlyList<Route> Routes { get { return new ReadOnlyCollection<Route>(_routes); } } 

        public RouteTable(IContainer container)
        {
            _routes = new List<Route>();
            _container = container;
        }

        public void Add(string url, object defaults)
        {
            Add(new Route(_container, url, defaults));
        }

        public void Add(Route route)
        {
            _routes.Add(route);
        }

        public Route Match(string url, out IDictionary<string, object> routeValues)
        {
            foreach (var route in _routes)
            {
                if (route.Match(url, out routeValues))
                    return route;
            }
            routeValues = null;
            return null;
        }
    }
}