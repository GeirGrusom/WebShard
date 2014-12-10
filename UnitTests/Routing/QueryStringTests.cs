using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WebShard.Routing;

namespace UnitTests.Routing
{
    [TestFixture]
    public class QueryStringTests
    {
        [Test]
        public void Parse_Ok()
        {
            // Arrange
            var qr = "foo=bar";
            
            // Act
            var dict = QueryString.Parse(qr);

            // Assert
            Assert.That(dict.ContainsKey("foo"), Is.True);
            Assert.That(dict["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Parse_ReturnedDictionary_IsCase_Insensitive()
        {
            // Arrange
            var qr = "foo=bar";

            // Act
            var dict = QueryString.Parse(qr);

            // Assert
            Assert.That(dict.ContainsKey("FOO"), Is.True);
        }

        [Test]
        public void Parse_MultipleValues_Ok()
        {
            // Arrange
            var qr = "foo=bar&hihi=hoho";

            // Act
            var dict = QueryString.Parse(qr);

            // Assert
            Assert.That(dict.ContainsKey("foo"), Is.True);
            Assert.That(dict.ContainsKey("hihi"), Is.True);
        }

        [Test]
        public void Parse_EscapesKey_Ok()
        {
            // Arrange
            var qr = "foo+bar=biz";

            // Act
            var dict = QueryString.Parse(qr);

            // Assert
            Assert.That(dict.ContainsKey("foo bar"), Is.True);
        }

        [Test]
        public void Parse_EscapesValue_Ok()
        {
            // Arrange
            var qr = "foo=fizz+buzz";

            // Act
            var dict = QueryString.Parse(qr);

            // Assert
            Assert.That(dict["foo"], Is.EqualTo("fizz buzz"));
        }

        public class SingleLevelType<T>
        {
            public T Value { get; set; }
        }

        [Test]
        public void TryDeserialize_String_Ok()
        {
            // Arrange

            // Act
            SingleLevelType<string> result;
            bool status = QueryString.TryDeserialize("Value=Foo", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo("Foo"));
        }

        [Test]
        public void TryDeserialize_Int_Ok()
        {
            // Arrange

            // Act
            SingleLevelType<int> result;
            bool status = QueryString.TryDeserialize("Value=10", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo(10));
        }

        [Test]
        public void TryDeserialize_Float_Ok()
        {
            // Arrange

            // Act
            SingleLevelType<float> result;
            bool status = QueryString.TryDeserialize("Value=10", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo(10.0f));
        }

        [Test]
        public void TryDeserialize_Double_Ok()
        {
            // Arrange

            // Act
            SingleLevelType<double> result;
            bool status = QueryString.TryDeserialize("Value=10", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo(10.0));
        }

        [Test]
        public void TryDeserialize_Decimal_Ok()
        {
            // Arrange

            // Act
            SingleLevelType<decimal> result;
            bool status = QueryString.TryDeserialize("Value=10", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo(10m));
        }

        [Test]
        public void TryDeserialize_NullableValueType_EmptyString_ReturnsNull()
        {
            // Arrange

            // Act
            SingleLevelType<decimal?> result;
            bool status = QueryString.TryDeserialize("Value=", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo((decimal?)null));
        }

        [Test]
        public void TryDeserialize_NullableValueType_WithValue_ReturnsValue()
        {
            // Arrange

            // Act
            SingleLevelType<decimal?> result;
            bool status = QueryString.TryDeserialize("Value=10", out result);

            // Assert
            Assert.That(status, Is.True);
            Assert.That(result.Value, Is.EqualTo((decimal?)10m));
        }
    }
}
