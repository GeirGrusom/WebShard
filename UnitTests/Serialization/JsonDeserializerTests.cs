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
        [TestCase(typeof(object), "null", null)]
        [TestCase(typeof(object), "", null)]
        [TestCase(typeof(string), "", null)]
        [TestCase(typeof(string), "null", null)]
        [TestCase(typeof(string), "\"\"", "")]
        [TestCase(typeof(int?), "10", 10)]
        [TestCase(typeof(int?), "null", null)]
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

        public class ImmutableModel
        {
            private readonly string _value;

            public string Value { get { return _value; } }

            public ImmutableModel(string value)
            {
                _value = value;
            }
        }

        [Test]
        public void Deserialize_Object_UsingConstructorInjection()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<ImmutableModel>("{ \"Value\": \"Foo\" }");

            // Assert
            Assert.That(result.Value, Is.EqualTo("Foo"));
        }

        [Test]
        public void Deserialize_Object_UsingConstructorInjection_MissingMember()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = Assert.Catch<JsonDeserializationException>(() => json.Deserialize<ImmutableModel>("{ \"Not-Value-At-All\": \"Foo\" }"));

            // Assert
            Assert.That(result.Token, Is.EqualTo("\"Not-Value-At-All\""));
        }

        [Test]
        public void Deserialize_Object_UsingConstructorInjection_Incomplete_FillsWithDefaults()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<ImmutableModel>("{ }");

            // Assert
            Assert.That(result.Value, Is.EqualTo(null)); // Token is the one From the start of the object.
        }

        public class MutableModel
        {
            public string Value { get; set; }
        }

        [Test]
        public void Deserialize_Object_UsingWritableProperties_MissingLeftBrace_ThrowsJsonDeserializationException()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = Assert.Catch<JsonDeserializationException>(() => json.Deserialize<MutableModel>("[ \"Value\": \"Foo\" ]"));

            // Assert
            Assert.That(result.Token, Is.EqualTo("["));
        }

        [Test]
        public void Deserialize_Object_UsingWritableProperties()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<MutableModel>("{ \"Value\": \"Foo\" }");

            // Assert
            Assert.That(result.Value, Is.EqualTo("Foo"));
        }

        public class MutableClassWithConstructor
        {
            private readonly string _immutableValue;
            public string MutableValue { get; set; }
            public string ImmutableValue { get { return _immutableValue; } }

            public MutableClassWithConstructor()
            {
            }

            public MutableClassWithConstructor(string immutableValue)
            {
                _immutableValue = immutableValue;
            }
        }

        [Test]
        public void Deserialize_Object_PrefersConstructorInjection()
        {
            // Arrange
            var json = new JsonDeserializer();

            // Act
            var result = json.Deserialize<MutableClassWithConstructor>("{ \"ImmutableValue\": \"Foo\" }");

            // Assert
            Assert.That(result.ImmutableValue, Is.EqualTo("Foo"));
        }
    }
}
