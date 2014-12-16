using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WebShard;
using WebShard.Ioc;

namespace UnitTests
{
    [TestFixture]
    public class HttpApplicationTests
    {

        public class TestController
        {
            public IResponse Test()
            {
                return new ContentResponse("OK");
            }
        }

        [Test]
        public void RouteController_ReturnsOK()
        {
            // Arrange
            var app = new HttpApplication();
            var request = Substitute.For<IHttpRequestContext>();
            var headers = new HeaderCollection();
            request.Headers.Returns(headers);
            request.Uri.Returns(new Uri("http://test.com/test/test"));
            request.Headers.AcceptEncoding = new string[0];
            app.ControllerRegistry.Register<TestController>();
            app.RouteTable.Add("/{controller}/{action}", new object());

            // Act
            var response = app.ProcessRequest(request);

            // Assert
            Assert.That(response, Is.Not.Null);

            var responseMemory = new MemoryStream();
            response.WriteResponse(responseMemory);
            var contents = Encoding.UTF8.GetString(responseMemory.ToArray());
            Assert.That(contents, Is.StringEnding("OK"));
        }

        public class Foo
        {
            private readonly string _value;
            public string Value { get { return _value; } }

            public Foo(string value)
            {
                _value = value;
            }
        }
        public class DeserializeJsonTestController
        {
            public IResponse Index(Foo foo)
            {
                return new ContentResponse(foo.Value);       
            }
        }

        public class NormalGetControllerReturnsOk
        {
            public IResponse Get()
            {
                return new ContentResponse("OK");
            }
        }

        [Test]
        public void Filtering_Fails_ReturnsInternalServerError()
        {
            // Arrange
            var app = new HttpApplication();
            var req = Substitute.For<IHttpRequestContext>();
            var filter = Substitute.For<IRequestFilter>();
            filter.WhenForAnyArgs(x => x.Process()).Do(x =>  { throw new FormatException("Foobar"); });
            app.FilterRegistry.Register(c => filter);
            req.Uri.Returns(new Uri("http://www.example.com/"));
            req.Method.Returns("GET");
            req.Headers.Returns(new HeaderCollection());
            app.RouteTable.Add("/", new { action = new Func<IResponse>(() => new StatusResponse(Status.Ok) ) });

            // Act
            var response = app.ProcessRequest(req);

            // Assert
            Assert.That(response.Status.Code == 500);
        }

        class ThrowingController
        {
            public IResponse Get()
            {
                throw new FormatException();
            }
        }
        [Test]
        public void Controller_Fails_ReturnsInternalServerError()
        {
            // Arrange
            bool isExceptionEventRaised = false;
            var app = new HttpApplication();
            app.ApplicationException += (application, ev) => isExceptionEventRaised = true;
            var req = Substitute.For<IHttpRequestContext>();
            req.Uri.Returns(new Uri("http://www.example.com/"));
            req.Method.Returns("GET");
            req.Headers.Returns(new HeaderCollection());
            app.ControllerRegistry.Register<ThrowingController>();
            app.RouteTable.Add("/", new { controller = "Throwing" });

            // Act
            var response = app.ProcessRequest(req);

            // Assert
            Assert.That(response.Status.Code == 500);
            Assert.That(isExceptionEventRaised, Is.True);
        }

        [Test]
        public void Action_Fails_ReturnsInternalServerError()
        {
            // Arrange
            var app = new HttpApplication();
            var req = Substitute.For<IHttpRequestContext>();
            req.Uri.Returns(new Uri("http://www.example.com/"));
            req.Method.Returns("GET");
            req.Headers.Returns(new HeaderCollection());
            app.RouteTable.Add("/", new { action = new Func<IResponse>(() => { throw new FormatException(); }) });

            // Act
            var response = app.ProcessRequest(req);

            // Assert
            Assert.That(response.Status.Code == 500);
        }

        [Test]
        public void ControllerExplicitlySet_Ok()
        {
            // Arrange
            var app = new HttpApplication();
            var req = Substitute.For<IHttpRequestContext>();
            req.Uri.Returns(new Uri("http://www.example.com/"));
            req.Method.Returns("GET");
            app.ControllerRegistry.Register<NormalGetControllerReturnsOk>();
            req.Headers.Returns(new HeaderCollection());
            app.RouteTable.Add("/", new { controller = typeof(NormalGetControllerReturnsOk) });

            // Act
            var response = app.ProcessRequest(req);

            // Assert
            var responseMemory = new MemoryStream();
            response.WriteResponse(responseMemory);
            var contents = Encoding.UTF8.GetString(responseMemory.ToArray());
            Assert.That(contents, Is.StringEnding("OK"));
        }
        

        [Test]
        public void FuncInvoke_InvokesTheFunc()
        {
            // Arrange
            var app = new HttpApplication();
            var req = Substitute.For<IHttpRequestContext>();
            req.Uri.Returns(new Uri("http://www.example.com/"));
            req.Headers.Returns(new HeaderCollection());
            app.RouteTable.Add("/", new { action = new Func<IResponse>(() => new ContentResponse("OK")) });

            // Act
            var response = app.ProcessRequest(req);

            // Assert
            var responseMemory = new MemoryStream();
            response.WriteResponse(responseMemory);
            var contents = Encoding.UTF8.GetString(responseMemory.ToArray());
            Assert.That(contents, Is.StringEnding("OK"));
        }

        [Test]
        public void FuncInvoke_IHttpRequest_InvokesTheFunc()
        {
            // Arrange
            var app = new HttpApplication();
            var req = Substitute.For<IHttpRequestContext>();
            req.Uri.Returns(new Uri("http://www.example.com/"));
            req.Headers.Returns(new HeaderCollection());
            app.RouteTable.Add("/", new { action = new Func<IHttpRequestContext, IResponse>(r => new ContentResponse("OK")) });

            // Act
            var response = app.ProcessRequest(req);

            // Assert
            var responseMemory = new MemoryStream();
            response.WriteResponse(responseMemory);
            var contents = Encoding.UTF8.GetString(responseMemory.ToArray());
            Assert.That(contents, Is.StringEnding("OK"));
        }

        [Test]
        public void ActionInvoker_WithJsonBody_DeserializesJson()
        {
            // Arrange
            var app = new HttpApplication();
            var request = Substitute.For<IHttpRequestContext>();
            var headers = new HeaderCollection();
            request.Headers.Returns(headers);
            request.Uri.Returns(new Uri("http://test.com/"));
            headers.AcceptEncoding = new string[0];
            headers.ContentType = "application/json";
            request.Body.Returns(new MemoryStream(Encoding.UTF8.GetBytes("{ \"Value\": \"Bar\"}")));
            app.ControllerRegistry.Register<DeserializeJsonTestController>();
            app.RouteTable.Add("/", new { controller = "DeserializeJsonTest", action = "Index" });

            // Act
            var response = app.ProcessRequest(request);

            // Assert
            var responseMemory = new MemoryStream();
            response.WriteResponse(responseMemory);
            var contents = Encoding.UTF8.GetString(responseMemory.ToArray());
            Assert.That(contents, Is.StringEnding("Bar"));
        }

        [Test]
        public void RouteController_NoMatch_ReturnsNotFound()
        {
            // Arrange
            var app = new HttpApplication();
            var request = Substitute.For<IHttpRequestContext>();
            var headers = new HeaderCollection();
            request.Headers.Returns(headers);
            request.Uri.Returns(new Uri("http://test.com/notfound"));
            request.Headers.AcceptEncoding = new string[0];
            app.ControllerRegistry.Register<TestController>();
            app.RouteTable.Add("/{controller}/{action}", new object());

            // Act
            var response = app.ProcessRequest(request);

            // Assert
            Assert.That(response, Is.Not.Null);

            var responseMemory = new MemoryStream();
            response.WriteResponse(responseMemory);
            var contents = Encoding.UTF8.GetString(responseMemory.ToArray());
            Assert.That(contents, Is.StringStarting("HTTP/1.1 404 Not found"));
        }
    }
}
