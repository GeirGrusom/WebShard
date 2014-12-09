using System;
using System.Threading;
using NSubstitute;
using NUnit.Framework;
using WebShard;
using WebShard.Ioc;
// ReSharper disable once ClassNeverInstantiated.Local
namespace UnitTests.Ioc
{
    [TestFixture]
    public class ContainerTests
    {

        private class Clonable : ICloneable
        {
            public object Clone()
            {
                throw new NotSupportedException();
            }
        }


        [Test]
        public void Registers_ThreadUnique_Ok()
        {
            // Arrange
            var container = new Container();

            ICloneable otherThreadValue = null;
            container.For<ICloneable>().Use<Clonable>(Lifetime.Thread);
            var value = container.Get<ICloneable>();

            var otherThread = new Thread(() => otherThreadValue = container.Get<ICloneable>());
            otherThread.Start();

            while(otherThread.IsAlive)
                Thread.Sleep(0);

            Assert.That(otherThreadValue, Is.Not.Null);
            Assert.That(value, Is.Not.SameAs(otherThreadValue));
            Assert.That(container.Get<ICloneable>(), Is.SameAs(container.Get<ICloneable>()));
        }
        [Test]
        public void Registers_ReturnsNewInstance()
        {
            // Arrange
            var container = new Container();
            container.For<ICloneable>().Use<Clonable>();

            // Act
            var result = container.Get<ICloneable>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }
        private class DependsOnIDisposable : ICloneable
        {
            public IDisposable Disposable { get; private set; }

            public DependsOnIDisposable(IDisposable disposable)
            {
                Disposable = disposable;
            }
            public object Clone()
            {
                throw new NotSupportedException();
            }
        }

        private class Foo : IDisposable
        {
            public void Dispose()
            {
                throw new NotSupportedException();
            }
        }

        [Test]
        public void Registers_Prov_ReturnsValue()
        {
            // Arrange
            var value = Substitute.For<ICloneable>();
            var container = new Container();
            container.For<ICloneable>().Use(() => value);

            // Act
            var result = container.Get<ICloneable>();

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public void Registers_TypeDependsOnOther_Ok()
        {
            // Arrange
            var container = new Container();
            container.For<ICloneable>().Use<DependsOnIDisposable>();
            container.For<IDisposable>().Use<Foo>();

            // Act
            var result = container.Get<ICloneable>();

            // Assert
            Assert.That(result, Is.InstanceOf<DependsOnIDisposable>());
            var dep = (DependsOnIDisposable) result;
            Assert.That(dep.Disposable, Is.Not.Null);
        }

        [Test]
        public void Registers_NoLifetime_ReturnsNewInstance_BothTimes()
        {
            // Arrange
            var container = new Container();
            container.For<ICloneable>().Use<Clonable>();

            // Act
            var a = container.Get<ICloneable>();
            var b = container.Get<ICloneable>();

            // Assert
            Assert.That(a, Is.Not.Null);
            Assert.That(b, Is.Not.Null);
            Assert.That(a, Is.Not.EqualTo(b));
        }
        
        [Test]
        public void Registers_ApplicationLifetime_ReturnsTheSameInstance_Twice()
        {
            // Arrange
            var container = new Container();
            container.For<ICloneable>().Use<Clonable>(Lifetime.Application);

            // Act
            var a = container.Get<ICloneable>();
            var b = container.Get<ICloneable>();

            // Assert
            Assert.That(a, Is.Not.Null);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void Dispose_CallsDispose_OnCachedObjects_ApplicationLifetime()
        {
            var obj = Substitute.For<IDisposable>();
            var container = new Container();

            container.For<IDisposable>().Use(() => obj, Lifetime.Application);
            container.Get<IDisposable>();

            container.Dispose();

            obj.Received().Dispose();
        }

        [Test]
        public void Dispose_CallsDispose_OnCachedObjects_ThreadLifetime()
        {
            var obj = Substitute.For<IDisposable>();
            var container = new Container();

            container.For<IDisposable>().Use(() => obj, Lifetime.Thread);
            container.Get<IDisposable>();

            container.Dispose();

            obj.Received().Dispose();
        }

        private class TestController
        {
            
        }

        [Test]
        public void Get_T_ReturnsType()
        {
            // Arrange
            var container = new Container();
            container.Register<TestController>();

            // Act
            var result = container.Get<TestController>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Get_T_FromProxy_ReturnsValueForThisContainer()
        {
            // Arrange
            var container = new Container();
            var firstChildContainer = container.CreateChildContainer();
            firstChildContainer.For<IDisposable>().Use(() => Substitute.For<IDisposable>());
            var secondChildContainer = container.CreateChildContainer();
            secondChildContainer.Register<DependsOnIDisposable>();
            var proxy = secondChildContainer.CreateProxyContainer(firstChildContainer);

            // Act
            var result = proxy.Get<DependsOnIDisposable>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Get_T_NotDefined_ThrowsTypeDefinitionNotFoundException()
        {
            // Arrange
            var container = new Container();

            // Act
            var result = Assert.Catch<TypeDefinitionNotFoundException>(() => container.Get<TestController>());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type, Is.EqualTo(typeof(TestController)));
        }


        [Test]
        public void Get_ByName_ReturnsType()
        {
            // Act
            var container = new Container();
            container.Register<TestController>();

            // Arrange
            var result = container.Get("TestController");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Get_ByName_DoesNotExist_ThrowsTypeDefinitionNotFoundException()
        {
            // Act
            var container = new Container();

            // Arrange
            var result = Assert.Catch<TypeDefinitionNotFoundException>(() => container.Get("TestController"));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Message, Is.EqualTo("Could not locate a type defined with the name 'TestController'."));
            
        }

    }
}
