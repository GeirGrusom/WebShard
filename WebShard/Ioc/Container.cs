using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace WebShard.Ioc
{
    public sealed class Container : IContainer
    {
        private readonly ConcurrentDictionary<Type, Definition> _typeMap;
        private readonly IContainer _parent;
        private bool _isDisposed;

        

        private Container(IContainer parent, ConcurrentDictionary<Type, Definition> typeMap)
        {
            _parent = parent;
            _typeMap = typeMap;
        }

        private Container(IContainer parent)
            : this()
        {
            _parent = parent;
        }

        public Container()
        {
            _typeMap = new ConcurrentDictionary<Type, Definition>();
        }

        public IContainer Parent { get { return _parent; } }

        public IEnumerable<T> GetAll<T>(bool recurse = true)
            where T : class
        {
            var results = _typeMap.Where(e => typeof(T).IsAssignableFrom(e.Key))
                .Select(element => (T)element.Value.Get(this));
            if(recurse)
                return results.Concat(_parent.GetAll<T>());
            return results;
        }

        private interface IDefinition<out T>
            where T : class
        {
            T Get(IContainer container);
        }

        public IContainer CreateChildContainer()
        {
            return new Container(this);
        }

        /// <summary>
        /// Creates a proxy with a different parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IContainer CreateProxyContainer(IContainer parent)
        {
            return new Container(parent, _typeMap);
        }


        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            foreach (var item in _typeMap)
            {
                item.Value.Dispose();
            }
        }

        private abstract class CacheLifetime : IDisposable
        {
            protected object CachedValue;

            public abstract bool HasCacheExpired();

            public abstract void SetCachedObject(object value);

            public virtual void Dispose()
            {
                if(CachedValue is IDisposable)
                    ((IDisposable)CachedValue).Dispose();
                CachedValue = null;
            }

            public virtual object GetCachedValue()
            {
                return CachedValue;
            }

            public object TryGetCachedValue()
            {
                if (HasCacheExpired())
                    return null;
                return GetCachedValue();
            }
        }

        private sealed class ThreadCacheLifetime : CacheLifetime
        {
            private readonly ConcurrentDictionary<Thread, object> _objectStore;

            public ThreadCacheLifetime()
            {
                _objectStore = new ConcurrentDictionary<Thread, object>();
            }

            public override void Dispose()
            {
                foreach (var item in _objectStore.Select(i => i.Value).OfType<IDisposable>())
                {
                    item.Dispose();
                }
                _objectStore.Clear();
            }

            public override bool HasCacheExpired()
            {
                // Clean up threads.
                var deadThreads = _objectStore.Keys.Where(k => !k.IsAlive);
                foreach (var thread in deadThreads)
                {
                    object value;
                    _objectStore.TryRemove(thread, out value);
                }

                object result;
                _objectStore.TryGetValue(Thread.CurrentThread, out result);

                return result == null;
            }

            public override object GetCachedValue()
            {
                object result;
                _objectStore.TryGetValue(Thread.CurrentThread, out result);
                return result;
            }

            public override void SetCachedObject(object value)
            {
                _objectStore.TryAdd(Thread.CurrentThread,  value);
            }
        }

        private sealed class ApplicationCacheLifetime : CacheLifetime
        {
            public override bool HasCacheExpired()
            {
                return false;
            }

            public override void SetCachedObject(object value)
            {
                CachedValue = value;
            }
        }

        private sealed class NoCacheLifetime : CacheLifetime
        {
            public static readonly CacheLifetime Instance = new NoCacheLifetime();

            private NoCacheLifetime()
            {
            }

            public override void Dispose()
            {
            }

            public override bool HasCacheExpired()
            {
                return true;
            }

            public override void SetCachedObject(object value)
            {
                
            }
        }

        private class Definition : IDefinition<object>, IDisposable
        {
            private readonly Func<IContainer, object> _createProc;
            private readonly CacheLifetime _cacheLifetime;

            public void Dispose()
            {
                if (_cacheLifetime != null)
                    _cacheLifetime.Dispose();
                
            }

            protected Definition(Func<IContainer, object> createProc, CacheLifetime cacheLifetime)
            {
                _cacheLifetime = cacheLifetime ?? NoCacheLifetime.Instance;
                _createProc = createProc;
            }

            public object Get(IContainer container)
            {
                var cached = _cacheLifetime.TryGetCachedValue();
                if (cached == null)
                {
                    cached = _createProc(container);
                    _cacheLifetime.SetCachedObject(cached);
                }
                return cached;
            }
        }

        private class Definition<T> : Definition, IDefinition<T> 
            where T : class
        {
            public Definition(Func<IContainer, T> createProc, CacheLifetime cacheLifetime)
                : base(createProc, cacheLifetime)
            {
                
            }

            public new T Get(IContainer container)
            {
                return (T)base.Get(container);
            }
        }

        private sealed class DefineType : IDefineType
        {
            private readonly Type _defineFor;
            private readonly Container _container;
            public DefineType(Type type, Container container)
            {
                _defineFor = type;
                _container = container;
            }

            private Func<IContainer, T> CreateFunc<T>()
            {
                var t = typeof (T);
                var constructor = t.GetConstructors()
                    .Select(c => new {Constructor = c, Parameters = c.GetParameters()})
                    .OrderByDescending(c => c.Parameters.Length).First();

                var containerType = typeof(IContainer);
                var method = containerType.GetMethod("Get");
                var container = Expression.Parameter(typeof (IContainer), "container");
                var func =Expression.Lambda<Func<IContainer, T>>(
                    Expression.New(constructor.Constructor,
                        constructor.Parameters.Select(
                            p =>
                                Expression.Call(container,
                                    method.MakeGenericMethod(p.ParameterType)))), container).Compile();

                return func;
            }

            private Func<IContainer, object> CreateFunc(Type t)
            {
                
                var constructor = t.GetConstructors()
                    .Select(c => new { Constructor = c, Parameters = c.GetParameters() })
                    .OrderByDescending(c => c.Parameters.Length).First();

                var containerType = typeof(IContainer);
                var method = containerType.GetMethod("Get", new Type[] {});
                var container = Expression.Parameter(typeof (IContainer), "container");
                var func = (Func<IContainer, object>)Expression.Lambda(
                    Expression.GetFuncType(typeof(IContainer), _defineFor),
                    Expression.New(constructor.Constructor,
                        constructor.Parameters.Select(
                            p =>
                                Expression.Call(container,
                                    method.MakeGenericMethod(p.ParameterType)))), container).Compile();

                return func;
            }

            private CacheLifetime CreateLifetime(Lifetime lf)
            {
                switch (lf)
                {
                    case Lifetime.None:
                        return NoCacheLifetime.Instance;
                    case Lifetime.Application:
                        return new ApplicationCacheLifetime();
                    /*case Lifetime.Request:
                        return new RequestCacheLifetime(_container);*/
                    case Lifetime.Thread:
                        return new ThreadCacheLifetime();
                    default:
                        throw new ArgumentException("Lifetime must be one of the defined values.");
                }
            }

            public void Use(Type type, Lifetime cacheLifetime = Lifetime.None)
            {
                _container._typeMap.AddOrUpdate(_defineFor, t => (Definition)Activator.CreateInstance(typeof(Definition<>).MakeGenericType(_defineFor), CreateFunc(type), CreateLifetime(cacheLifetime)), (a, b) => { throw new Exception(); });
            }

            public void Use<T>(Lifetime cacheLifetime = Lifetime.None)
                where T : class
            {
                Use(typeof(T), cacheLifetime);//_container.typeMap.AddOrUpdate(_defineFor, t => new Definition<T>(CreateFunc<T>(), CreateLifetime(cacheLifetime)), (a, b) => { throw new Exception(); });
            }

            public void Use<T>(Func<T> proc, Lifetime cacheLifetime = Lifetime.None)
                where T : class
            {
                _container._typeMap.AddOrUpdate(_defineFor, t => new Definition<T>(cont => proc(), CreateLifetime(cacheLifetime)), (a, b) => new Definition<T>(c => proc(), CreateLifetime(cacheLifetime)));
            }
        }

        public object GetService(Type serviceType)
        {
            Definition def;
            if (_typeMap.TryGetValue(serviceType, out def))
            {
                return def.Get(this);
            }
            if (_parent != null)
                return _parent.GetService(serviceType);
            return null;
        }

        public object Get(string name)
        {
            var v = _typeMap.FirstOrDefault(x => string.Equals(x.Key.Name, name, StringComparison.OrdinalIgnoreCase));
            if (v.Value != null)
            {
                return v.Value.Get(this);
            }

            if (_parent != null)
                return _parent.Get(name);

            throw new TypeDefinitionNotFoundException(null, "Could not locate a type defined with the name '" + name + "'.");
        }

        public object TryGet(string name)
        {
            var v = _typeMap.FirstOrDefault(x => string.Equals(x.Key.Name, name, StringComparison.OrdinalIgnoreCase));
            if (v.Value != null)
            {
                return v.Value.Get(this);
            }

            if (_parent != null)
                return _parent.TryGet(name);

            return null;
        }

        public T Get<T>() where T : class
        {
            Definition def;
            if (_typeMap.TryGetValue(typeof (T), out def))
            {
                return ((Definition<T>) def).Get(this);
            }
            if (_parent != null)
                return _parent.Get<T>();
            throw new TypeDefinitionNotFoundException(typeof(T));
        }

        public T TryGet<T>()
            where T : class
        {
            Definition def;
            if(_typeMap.TryGetValue(typeof(T), out def))
            {
                return ((Definition<T>) def).Get(this);
            }
            if(_parent != null)
                return _parent.TryGet<T>();
            return null;
        }

        public IDefineType For<TInterface>() where TInterface : class
        {
            return new DefineType(typeof(TInterface), this);
        }

        public IDefineType For(Type type)
        {
            return new DefineType(type, this);
        }
    }
}
