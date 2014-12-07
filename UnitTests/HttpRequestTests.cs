using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using WebShard;

namespace UnitTests
{
    [TestFixture]
    public class HttpRequestTests
    {
        [Test]
        public void MinimalHeader_Ok()
        {
            const string test = 
@"GET /wiki/Hypertext_Transfer_Protocol HTTP/1.1
Host: en.wikipedia.org

";
            var request = HttpRequestContext.CreateFromStream(new MemoryStream(Encoding.UTF8.GetBytes(test)), Schemes.Http, "::1");

            Assert.That(request.Method, Is.EqualTo(HttpMethods.Get));
            Assert.That(request.Uri, Is.EqualTo(new Uri("http://en.wikipedia.org/wiki/Hypertext_Transfer_Protocol")));
            Assert.That(request.ProtocolVersion, Is.EqualTo("HTTP/1.1"));
        }
    }
}
