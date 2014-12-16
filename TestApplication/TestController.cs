using System.Collections.Generic;
using System.Linq;

namespace WebShard
{
    public class TestController
    {
        public class Model
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
        private readonly IHttpRequestContext _request;
        private readonly UserRegistry _userRegistry;

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