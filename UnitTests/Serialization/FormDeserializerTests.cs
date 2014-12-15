using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WebShard.Serialization.Form;

namespace UnitTests.Serialization
{

    [TestFixture]
    public class FormDeserializerTests
    {
        public class FooBar
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
        [Test]
        public void SimpleObject_Ok()
        {
            // Arrange
            var form = new FormDeserializer();

            // Act
            var result = form.Deserialize<FooBar>("Key=Foo&Value=Bar");

            // Assert
            Assert.That(result.Key, Is.EqualTo("Foo"));
            Assert.That(result.Value, Is.EqualTo("Bar"));
        }
    }
}
