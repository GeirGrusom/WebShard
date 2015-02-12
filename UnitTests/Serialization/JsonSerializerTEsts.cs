using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WebShard.Serialization;

namespace UnitTests.Serialization
{
    
    [TestFixture]
    public class JsonSerializerTests
    {
        public enum TestEnum
        {
            Foo = 1
        }

        [TestCase(1, "1")]
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        [TestCase(1.1234f, "1.1234")]
        [TestCase(1.12345, "1.12345")]
        [TestCase("Hello World!", "\"Hello World!\"")]
        [TestCase(TestEnum.Foo, "Foo")]
        public void SerializePrimitive(object input, string result)
        {
            var json = new JsonSerializer();

            var output = json.Serialize(input);

            // Assert
            Assert.That(output, Is.EqualTo(result));
        }

        [Test]
        public void SerializeDecimal()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var output = json.Serialize(1.0m);

            // Assert
            Assert.That(output, Is.EqualTo("1.0"));
        }

        [Test]
        public void SerializeNull()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var result = json.Serialize(null);

            // Assert
            Assert.That(result, Is.EqualTo("null"));
        }

        [Test]
        public void SerializeNullable_IsNull()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var result = json.Serialize((int?) null);

            // Assert
            Assert.That(result, Is.EqualTo("null"));
        }

        [Test]
        public void SerializeNullable()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var result = json.Serialize((int?) 10);

            // Assert
            Assert.That(result, Is.EqualTo("10"));
        }

        [Test]
        public void SerializeArray()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var result = json.Serialize(new[] {1, 2, 3, 4});

            // Assert
            Assert.That(result, Is.EqualTo("[1, 2, 3, 4]"));

        }

        [Test]
        public void SerializeDictionary()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var result = json.Serialize(new Dictionary<int, int> { {4, 1 }, { 3, 2}, {2, 3}, {1, 4} });

            // Assert
            Assert.That(result, Is.EqualTo("{4: 1, 3: 2, 2: 3, 1: 4}"));

        }

        [Test]
        public void SerializeObject()
        {
            // Arrange
            var json = new JsonSerializer();

            // Act
            var result = json.Serialize(new {Foo = 123});

            // Assert
            Assert.That(result, Is.EqualTo("{\"Foo\": 123}"));
        }

    }
}
