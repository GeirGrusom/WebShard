using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WebShard;
using WebShard.Mvc;
using WebShard.Serialization.Form;

namespace UnitTests.Mvc
{



    [TestFixture]
    public class ActionInvokerTests
    {

        public class PostInput
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class PostResponse<T> : IResponse
        {
            public T Value { get; set; }
            public void Write(IHttpRequestContext request, IHttpResponseContext context)
            {
                throw new NotSupportedException();
            }
        }

        public class TestController
        {
            public IResponse Index()
            {
                return new StatusResponse(Status.Ok);
            }

            public IResponse Index(int id)
            {
                return StatusResponse.NotFound;
            }

            public IResponse Post(PostInput value)
            {
                return new PostResponse<PostInput> {Value = value};
            }
        }

        [Test]
        public void Invoke_Post_Ok()
        {
            // Arrange
            var actionInvoker = new ActionInvoker(typeof (TestController));
            var controller = new TestController();
            var request = Substitute.For<IHttpRequestContext>();
            request.Body.Returns(new MemoryStream(Encoding.UTF8.GetBytes("Name=Foo&Value=Bar")));

            // Act
            var result = (PostResponse<PostInput>)actionInvoker.Invoke(controller, request, new Dictionary<string, object> { { "action", "post" } }, new FormRequestDeserializer());

            // Assert
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("Foo"));
            Assert.That(result.Value.Value, Is.EqualTo("Bar"));
        }

        [Test]
        public void Invoke_NoArgument_Ok()
        {
            // Arrange
            var actionInvoker = new ActionInvoker(typeof(TestController));
            var controller = new TestController();

            // Act
            var result = actionInvoker.Invoke(controller, Substitute.For<IHttpRequestContext>(), new Dictionary<string, object> { { "action", "index" } });

            // Assert
            Assert.That(result, Is.InstanceOf<StatusResponse>());
        }

        [Test]
        public void Invoke_IntArgument_Ok()
        {
            // Arrange
            var actionInvoker = new ActionInvoker(typeof(TestController));
            var controller = new TestController();

            // Act
            var result = actionInvoker.Invoke(controller, Substitute.For<IHttpRequestContext>(),
                new Dictionary<string, object> { { "action", "index" }, { "id", "404" } });

            // Assert
            Assert.That(result, Is.InstanceOf<StatusResponse>());
            Assert.That(((StatusResponse)result).Status.Code, Is.EqualTo(404));
        }
    }
}
