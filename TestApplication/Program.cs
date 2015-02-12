using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using TestApplication;
using WebShard.Filtering;
using WebShard.Ioc;
using WebShard.Routing;

namespace WebShard
{

    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    /*public class PostedUser
    {
        private readonly string _firstName;
        private readonly string _lastName;
    }*/

    public class UserRegistry
    {
        private readonly ConcurrentBag<User> _users;

        public UserRegistry()
        {
            _users = new ConcurrentBag<User>();
        }

        public void AddUser(User user)
        {
            _users.Add(user);
        }

        public IEnumerable<User> GetUsers()
        {
            return _users;
        }
    }

    class Program
    {
        static void Main()
        {
            var app = new HttpApplication();

            app.FilterRegistry.Register(c => new PasswordCredentials("foo", "bar"));
            app.FilterRegistry.Register<BasicAuthorizationFilter>();
            app.FilterRegistry.For<IExceptionFilter>().Use<ErrorFilter>();
            app.ControllerRegistry.Register<TestController>();
            app.ControllerRegistry.Register<ResourcesController>();
            app.RouteTable.Add("/{action?}", new { controller = "Test" });
            app.RouteTable.Add(@"/{resourcePath:(css|js|fonts)}/{resourceName:[a-zA-Z][a-zA-Z0-9\.\-]*}", new { controller = "Resources" });
            app.Container.Register<UserRegistry>(Lifetime.Application);
            var server = new HttpWebServer(app);

            var th = new Thread(server.Start);
            th.Start();

            Console.ReadLine();

            server.Stop();
        }
    }
}
