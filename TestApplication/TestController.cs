using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebShard
{
    public class TestController
    {
        public class Model
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }

            public Model()
            {
                
            }

            public Model(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }
        private readonly IHttpRequestContext _request;
        private readonly UserRegistry _userRegistry;

        public static IResponse RenderException(Exception ex)
        {
            var result = new StringBuilder();

            result.AppendLine("<!doctype html>");
            result.AppendLine("<html>");
            result.AppendLine("<head>");
            result.AppendLine("<title>An error occured.</title>");
            result.AppendLine("</head>");
            result.AppendLine("<body>");
            result.AppendLine("<h4>An error occured while processing the request.</h4>");


            while (ex != null)
            {
                result.AppendFormat("<h5>An unhandled {0} was thrown</h5>", ex.GetType().Name);
                result.Append("<label>Message:</label><p>");
                result.AppendLine(ex.Message);
                result.Append("</p><label>Source:</label><p>");
                result.AppendLine(ex.Source);
                result.Append("</p><label>Stacktrace:</label><p>");
                result.AppendLine(ex.StackTrace);
                result.AppendLine("</p>");
                if (ex.InnerException != null)
                    result.AppendLine("<h4>Inner exception</h4>");
                ex = ex.InnerException;
            }
            result.AppendLine("</body>");
            result.AppendLine("</html>");
            return new ContentResponse(result.ToString());

        }

        public TestController(IHttpRequestContext request, UserRegistry userRegistry)
        {
            _request = request;
            _userRegistry = userRegistry;
        }

        public IResponse GetAllUsers()
        {
            return new JsonResponse<IEnumerable<User>>(_userRegistry.GetUsers());
        }

        public IResponse JsonTest()
        {
            return new JsonResponse(new { Foo = 123, Bar = new[] { 1, 2, 3 } });
        }

        public IResponse Post(Model model)
        {
            _userRegistry.AddUser(new User { FirstName = model.FirstName, LastName = model.LastName});
            
            return new RedirectResponse("/");
        }

        public IResponse Get()
        {
            return new FileSystemResponse(@".\Content\Index.html", "text/html", "utf-8");
        }
    }
}