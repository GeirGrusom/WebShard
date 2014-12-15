using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WebShard.Serialization.Json;

namespace UnitTests.Serialization
{
    [TestFixture]
    public class JsonDeserializerTests
    {
        class Foo
        {
            
        }
        [TestCase(typeof(int), "10", 10)]
        [TestCase(typeof(byte), "10", (byte)10)]
        [TestCase(typeof(short), "-10", (short)-10)]
        [TestCase(typeof(Foo), "", null)]
        [TestCase(typeof(string), "\"Foo\"", "Foo")]
        [TestCase(typeof(string), "Foo", "Foo")]
        [TestCase(typeof(bool), "true", true)]
        [TestCase(typeof(bool), "false", false)]
        public void Deserialize_Primitives(Type deserializeType, string expression, object expectedResult)
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize(expression, deserializeType);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Deserialize_Struct()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<KeyValuePair<string, string>>("{\"Key\":\"Foo\", \"Value\":\"Bar\"}");

            // Assert
            Assert.That(result.Key, Is.EqualTo("Foo"));
            Assert.That(result.Value, Is.EqualTo("Bar"));
        }

        [Test]
        public void Deserialize_Nullable()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<int?>("10");

            // Assert
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void Deserialize_Nullable_IsNull()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<int?>("null");

            // Assert
            Assert.That(result, Is.EqualTo(null));

        }

        [Test]
        public void Deserialize_IDictionary()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize("{\"Foo\": \"bar\", \"Baz\": \"Bat\"}", typeof(IDictionary<string, string>));

            // Assert
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string> { { "Foo", "bar" }, {"Baz", "Bat" }}));            
        }

        [Test]
        public void Deserialize_Dynamic_Dictionary()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            dynamic result = json.Deserialize("{\"Foo\": \"bar\"}");

            // Assert
            Assert.That(result.Foo, Is.EqualTo("bar"));
        }

        [Test]
        public void Deserialize_Dynamic_Array()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            dynamic result = json.Deserialize("[1, 2, 3]");

            // Assert
            Assert.That(result, Is.EquivalentTo(new []{1m, 2m, 3m}));
        }

        [TestCase("\"Foo\"", "Foo")]
        [TestCase("10", 10)]
        [TestCase("false", false)]
        [TestCase("true", true)]
        [TestCase("null", null)]
        public void Deserialize_Dynamic_Types(string expression, object expectedResult)
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            dynamic result = json.Deserialize(expression);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Deserialize_Dynamic_String()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            dynamic result = json.Deserialize("\"Foo\"");

            // Assert
            Assert.That(result, Is.EqualTo("Foo"));
        }

        [Test]
        public void Deserialize_IntArray()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = (int[])json.Deserialize("[1, 2, 3, 4]", typeof (int[]));

            // Assert
            Assert.That(result, Is.EquivalentTo(new [] { 1, 2, 3, 4}));
        }

        [Test]
        public void Deserialize_IEnumerable()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = (int[])json.Deserialize("[1, 2, 3, 4]", typeof(IEnumerable<int>));

            // Assert
            Assert.That(result, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
        }

    }
}
