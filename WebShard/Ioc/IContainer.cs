using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Ioc
{
    public enum Lifetime
    {
        /// <summary>
        /// A new instance is instantiated for every call.
        /// </summary>
        None,

        /// <summary>
        /// The container will create one instance and it will survive as long as the application is running.
        /// </summary>
        Application,

        /// <summary>
        /// The container will create one instance for the calling thread.
        /// </summary>
        Thread
    }
    public interface IDefineType
    {
        void Use(Type type, Lifetime lifetime = Lifetime.None);
        void Use<T>(Lifetime lifetime = Lifetime.None)
            where T : class;
        void Use<T>(Func<T> proc, Lifetime lifetime = Lifetime.None)
            where T : class;
    }

    public static class ContainerExtensions
    {
        public static void Register<T>(this IContainer container, Lifetime lifetime = Lifetime.None)
            where T : class
        {
            container.For<T>().Use<T>(lifetime);
        }

        public static void Register(this IContainer container, Type type, Lifetime lifetime = Lifetime.None)
        {
            container.For(type).Use(type, lifetime);
        }

        public static void Register<T>(this IContainer container, Func<T> proc , Lifetime lifetime = Lifetime.None)
            where T : class
        {
            container.For<T>().Use(proc, lifetime);
        }

    }

    public interface IContainer : IServiceProvider, IDisposable
    {
        T Get<T>()
            where T : class;

        T TryGet<T>()
            where T :  class;

        object Get(string typeName);

        object TryGet(string typeName);

        IDefineType For<TInterface>()
            where TInterface : class;

        IDefineType For(Type type);

        IContainer Parent { get; }

        IContainer CreateChildContainer();
    }
}
