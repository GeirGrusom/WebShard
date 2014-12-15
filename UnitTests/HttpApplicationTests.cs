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

        [Test, Ignore("WIP")]
        public void ActionInvoker_WithJsonBody_DeserializesJson()
        {
            // Arrange
            var app = new HttpApplication();
            var request = Substitute.For<IHttpRequestContext>();
            var headers = new HeaderCollection();
            request.Headers.Returns(headers);
            request.Uri.Returns(new Uri("http://test.com/"));
            request.Headers.AcceptEncoding.Returns( new string[0]);
            request.Body.Returns(new MemoryStream(Encoding.UTF8.GetBytes("{ \"Foo\": \"Bar\"}")));

            // Act
            var response = app.ProcessRequest(request);

            // Assert
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
