using System;
using System.Collections.Generic;

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
        Thread,

        /// <summary>
        /// Defines that the cache object will live as long as the request.
        /// </summary>
        /// <remarks>
        /// Note that this is functionally the same as <see cref="Lifetime.Application"/> for a single container.</remarks>
        Request,
    }
    public interface IDefineType
    {
        void Use(Type type, Lifetime lifetime = Lifetime.None);
        void Use<T>(Lifetime lifetime = Lifetime.None)
            where T : class;
        void Use<T>(Func<IContainer, T> proc, Lifetime lifetime = Lifetime.None)
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

        public static void Register<T>(this IContainer container, Func<IContainer, T> proc , Lifetime lifetime = Lifetime.None)
            where T : class
        {
            container.For<T>().Use(proc, lifetime);
        }

    }

    public interface IContainer : IServiceProvider, IDisposable
    {
        IEnumerable<T> GetAll<T>(bool recurse = true)
            where T : class;

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

        IContainer CreateRequestChildContainer();

        IContainer CreateProxyContainer(IContainer parent);
    }
}
