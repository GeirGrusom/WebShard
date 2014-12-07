using NUnit.Framework;
using WebShard;

namespace UnitTests
{
    [TestFixture]
    public class HttpHeaderCollectionTests
    {
        [Test]
        public void Add_String_String_ReturnsCorrectValue()
        {
            // Arrange
            var headerCollection = new HeaderCollection();

            // Act
            headerCollection.Add("foo", "bar");
            var result = headerCollection["foo"];

            // Assert
            Assert.That(result, Is.EqualTo("bar"));
        }

        [Test]
        public void Add_String_String_CaseInsensitive_ReturnsCorrectValue()
        {
            // Arrange
            var headerCollection = new HeaderCollection();

            // Act
            headerCollection.Add("FOO", "bar");
            var result = headerCollection["foo"];

            // Assert
            Assert.That(result, Is.EqualTo("bar"));
        }

        [Test]
        public void Get_NotAdded_ReturnsNull()
        {
            // Arrange
            var headerCollection = new HeaderCollection();

            // Act
            var result = headerCollection["foo"];

            // Assert
            Assert.That(result, Is.Null);
        }

        [TestFixture]
        public class DefaultHttpHeadersTests
        {
            [Test]
            public void Accepts_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    Accept = "foo"
                };

                // Act
                var result = headerCollection.Accept;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));

            }
            [Test]
            public void AcceptsCharset_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    AcceptCharset = "foo"
                };

                // Act
                var result = headerCollection.AcceptCharset;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void AcceptEncoding_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    AcceptEncoding = new []{"foo"}
                };

                // Act
                var result = headerCollection.AcceptEncoding;

                // Assert
                Assert.That(result, Is.EquivalentTo(new [] {"foo" }));

            }
            [Test]
            public void Authorization_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                { 
                    Authorization = "foo"
                };

                // Act
                var result = headerCollection.Authorization;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void Connection_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    Connection = "foo"
                };

                // Act
                var result = headerCollection.Connection;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void ContentEncoding_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    ContentEncoding = "foo"
                };

                // Act
                var result = headerCollection.ContentEncoding;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void ContentLength_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    ContentLength= 1000
                };

                // Act
                var result = headerCollection.ContentLength;

                // Assert
                Assert.That(result, Is.EqualTo(1000));
            }

            [Test]
            public void ContentType_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    ContentType = "foo"
                };

                // Act
                var result = headerCollection.ContentType;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void Cookie_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    Cookie = "foo"
                };

                // Act
                var result = headerCollection.Cookie;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void ETag_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    ETag = "foo"
                };

                // Act
                var result = headerCollection.ETag;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void Host_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    Host = "foo"
                };

                // Act
                var result = headerCollection.Host;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void IfNoneMatch_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    IfNoneMatch = "foo"
                };

                // Act
                var result = headerCollection.IfNoneMatch;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }

            [Test]
            public void UserAgent_Ok()
            {
                // Arrange
                var headerCollection = new HeaderCollection
                {
                    UserAgent = "foo"
                };

                // Act
                var result = headerCollection.UserAgent;

                // Assert
                Assert.That(result, Is.EqualTo("foo"));
            }
        }
    }
}
