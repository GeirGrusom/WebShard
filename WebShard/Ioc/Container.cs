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
        private readonly ConcurrentDictionary<Type, Definition> _requestTypeMap; // Contains a copy of all definitons with a Request lifetime. 
        private readonly ConcurrentDictionary<Type, object> _cache; 
        private readonly IContainer _parent;

        private bool _isDisposed;

        private Container(IContainer parent, ConcurrentDictionary<Type, Definition> typeMap)
        {
            _parent = parent;
            _typeMap = typeMap;
            _requestTypeMap = new ConcurrentDictionary<Type, Definition>();
            _cache = new ConcurrentDictionary<Type, object>();
        }

        private Container(IContainer parent)
            : this()
        {
            _parent = parent;
        }

        public Container()
        {
            _requestTypeMap = new ConcurrentDictionary<Type, Definition>();
            _cache = new ConcurrentDictionary<Type, object>();
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

        public IContainer CreateRequestChildContainer()
        {
            return new Container(this, _requestTypeMap);
        }


        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            foreach (var item in _typeMap)
            {
                item.Value.Dispose(this);
            }
        }

        private abstract class CacheLifetime : IDisposable
        {
            protected readonly Type Key;
            private object _cachedValue;

            protected CacheLifetime(Type key)
            {
                Key = key;
            }

            public abstract bool HasCacheExpired(IContainer container);

            public virtual void Dispose(IContainer container)
            {
                var c = container as Container;
                if (c != null)
                {
                    object value;
                    if (c._cache.TryRemove(Key, out value))
                    {
                        var disposable = value as IDisposable;
                        if (disposable != null)
                            disposable.Dispose();
                    }
                }
            }

            public virtual void SetCachedObject(IContainer container, object value)
            {
                var c = container as Container;
                if (c != null)
                {
                    c._cache.AddOrUpdate(Key,
                        t => value,
                        (t, o) =>
                        {
                            var disposable = o as IDisposable;
                            if (disposable != null && !ReferenceEquals(disposable, value))
                                disposable.Dispose();
                            return value;
                        });
                }
                _cachedValue = value;
            }

            public virtual void Dispose()
            {
                if(_cachedValue is IDisposable)
                    ((IDisposable)_cachedValue).Dispose();
                _cachedValue = null;
            }

            public virtual object GetCachedValue(IContainer container)
            {
                var c = container as Container;
                if (c != null)
                {
                    object result;
                    c._cache.TryGetValue(Key, out result);
                    return result;
                }
                return _cachedValue;
            }

            public object TryGetCachedValue(IContainer container)
            {
                if (HasCacheExpired(container))
                    return null;
                return GetCachedValue(container);
            }
        }

        private sealed class ThreadCacheLifetime : CacheLifetime
        {
            private readonly ConcurrentDictionary<Thread, object> _objectStore;

            public ThreadCacheLifetime(Type key)
                : base(key)
            {
                _objectStore = new ConcurrentDictionary<Thread, object>();
            }

            public override void Dispose(IContainer container)
            {
                ConcurrentDictionary<Thread, object> objectStore;
                var c = container as Container;
                if (c != null)
                {
                    objectStore =
                        (ConcurrentDictionary<Thread, object>)
                            c._cache.GetOrAdd(Key, t => new ConcurrentDictionary<Thread, object>());
                }
                else
                {
                    objectStore = _objectStore;
                }

                foreach (var item in objectStore.Select(i => i.Value).OfType<IDisposable>())
                {
                    item.Dispose();
                }
                _objectStore.Clear();
            }

            public override bool HasCacheExpired(IContainer container)
            {
                // Clean up threads.
                var objectStore = container is Container ? (ConcurrentDictionary<Thread, object>)base.GetCachedValue(container) : _objectStore;

                if (objectStore == null)
                    return true;

                var deadThreads = objectStore.Keys.Where(k => !k.IsAlive);
                foreach (var thread in deadThreads)
                {
                    object value;
                    objectStore.TryRemove(thread, out value);
                }

                object result;
                objectStore.TryGetValue(Thread.CurrentThread, out result);

                return result == null;
            }

            public override object GetCachedValue(IContainer container)
            {
                var objectStore = (ConcurrentDictionary<Thread, object>)base.GetCachedValue(container);
                if (objectStore == null)
                    return null;
                object result;
                objectStore.TryGetValue(Thread.CurrentThread, out result);
                return result;
            }

            public override void SetCachedObject(IContainer container, object value)
            {
                ConcurrentDictionary<Thread, object> objectStore;
                var c = container as Container;
                if (c != null)
                {
                    objectStore =
                        (ConcurrentDictionary<Thread, object>)
                            c._cache.GetOrAdd(Key, t => new ConcurrentDictionary<Thread, object>());
                }
                else
                {
                    objectStore = _objectStore;
                }

                objectStore.AddOrUpdate(Thread.CurrentThread, t => value, (t, o) =>
                {
                    var disposable = o as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                    return value;
                });
            }
        }

        private sealed class ApplicationCacheLifetime : CacheLifetime
        {
            public ApplicationCacheLifetime(Type key)
                : base(key)
            {
            }

            public override bool HasCacheExpired(IContainer container)
            {
                return false;
            }
        }

        private sealed class RequestCacheLifetime : CacheLifetime
        {
            public RequestCacheLifetime(Type key)
                : base(key)
            {
            }

            public override bool HasCacheExpired(IContainer container)
            {
                return false;
            }
        }

        private sealed class NoCacheLifetime : CacheLifetime
        {
            public static readonly CacheLifetime Instance = new NoCacheLifetime(null);

            private NoCacheLifetime(Type key)
                : base(key)
            {
            }

            public override void Dispose()
            {
            }

            public override bool HasCacheExpired(IContainer container)
            {
                return true;
            }

            public override void SetCachedObject(IContainer container, object value)
            {
                
            }
        }

        private class Definition : IDefinition<object>
        {
            private readonly Func<IContainer, object> _createProc;
            private readonly CacheLifetime _cacheLifetime;
            public void Dispose(IContainer container)
            {
                if (_cacheLifetime != null)
                    _cacheLifetime.Dispose(container);
                
            }

            protected Definition(Func<IContainer, object> createProc, CacheLifetime cacheLifetime)
            {
                _cacheLifetime = cacheLifetime ?? NoCacheLifetime.Instance;
                _createProc = createProc;
            }

            public object Get(IContainer container)
            {
                var cached = _cacheLifetime.TryGetCachedValue(container);
                if (cached == null)
                {
                    cached = _createProc(container);
                    _cacheLifetime.SetCachedObject(container, cached);
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

            private CacheLifetime CreateLifetime(Type key, Lifetime lf)
            {
                switch (lf)
                {
                    case Lifetime.None:
                        return NoCacheLifetime.Instance;
                    case Lifetime.Application:
                        return new ApplicationCacheLifetime(key);
                    case Lifetime.Request:
                        return new RequestCacheLifetime(key);
                    case Lifetime.Thread:
                        return new ThreadCacheLifetime(key);
                    default:
                        throw new ArgumentException("Lifetime must be one of the defined values.");
                }
            }

            public void Use(Type type, Lifetime cacheLifetime = Lifetime.None)
            {
                var def = _container._typeMap.AddOrUpdate(_defineFor, t => (Definition)Activator.CreateInstance(typeof(Definition<>).MakeGenericType(_defineFor), CreateFunc(type), CreateLifetime(_defineFor, cacheLifetime)), (a, b) => { throw new Exception(); });
                if (cacheLifetime == Lifetime.Request)
                {
                    _container._requestTypeMap.AddOrUpdate(type, def, (t, d) => def);
                }
            }

            public void Use<T>(Lifetime cacheLifetime = Lifetime.None)
                where T : class
            {
                Use(typeof(T), cacheLifetime);//_container.typeMap.AddOrUpdate(_defineFor, t => new Definition<T>(CreateFunc<T>(), CreateLifetime(cacheLifetime)), (a, b) => { throw new Exception(); });
            }

            public void Use<T>(Func<T> proc, Lifetime cacheLifetime = Lifetime.None)
                where T : class
            {
                var def = _container._typeMap.AddOrUpdate(_defineFor, t => new Definition<T>(cont => proc(), CreateLifetime(_defineFor, cacheLifetime)), (a, b) => new Definition<T>(c => proc(), CreateLifetime(_defineFor, cacheLifetime)));
                if (cacheLifetime == Lifetime.Request)
                {
                    _container._requestTypeMap.AddOrUpdate(_defineFor, def, (t, d) => def);
                }
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
