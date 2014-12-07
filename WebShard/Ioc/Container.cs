using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebShard.Ioc
{
    
    public sealed class Container : IContainer
    {
        private readonly ConcurrentDictionary<Type, Definition> typeMap;
        private readonly IContainer _parent;
        private bool _isDisposed;

        private Container(IContainer parent)
            : this()
        {
            _parent = parent;
        }

        public Container()
        {
            typeMap = new ConcurrentDictionary<Type, Definition>();
        }

        public IContainer Parent { get { return _parent; } }

        private interface IDefinition<out T>
            where T : class
        {
            T Get();
        }

        public IContainer CreateChildContainer()
        {
            return new Container(this);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            foreach (var item in this.typeMap)
            {
                item.Value.Dispose();
            }
        }

        private abstract class CacheLifetime : IDisposable
        {
            protected readonly IContainer Container;

            protected object CachedValue;

            protected CacheLifetime(IContainer container)
            {
                Container = container;
            }

            public abstract bool HasCacheExpired();

            public abstract void SetCachedObject(object value);

            public virtual void Dispose()
            {
                if(CachedValue is IDisposable)
                    ((IDisposable)CachedValue).Dispose();
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
            private readonly ConcurrentDictionary<Thread, object> objectStore;

            public ThreadCacheLifetime(IContainer container)
                : base(container)
            {
                objectStore = new ConcurrentDictionary<Thread, object>();
            }

            public override void Dispose()
            {
                foreach (var item in objectStore.Select(i => i.Value).OfType<IDisposable>())
                {
                    item.Dispose();
                }
            }

            public override bool HasCacheExpired()
            {
                // Clean up threads.
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

            public override object GetCachedValue()
            {
                object result;
                objectStore.TryGetValue(Thread.CurrentThread, out result);
                return result;
            }

            public override void SetCachedObject(object value)
            {
                objectStore.TryAdd(Thread.CurrentThread,  value);
            }
        }

        private sealed class ApplicationCacheLifetime : CacheLifetime
        {
            public ApplicationCacheLifetime()
                : base(null)
            {
                
            }

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

            public NoCacheLifetime()
                : base(null)
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
            private readonly Func<object> _createProc;
            private readonly CacheLifetime _cacheLifetime;

            public void Dispose()
            {
                if (_cacheLifetime != null)
                    _cacheLifetime.Dispose();
                
            }

            public Definition(Func<object> createProc, CacheLifetime cacheLifetime)
            {
                this._cacheLifetime = cacheLifetime ?? NoCacheLifetime.Instance;
                this._createProc = createProc;
            }

            public object Get()
            {
                var cached = _cacheLifetime.TryGetCachedValue();
                if (cached == null)
                {
                    cached = _createProc();
                    _cacheLifetime.SetCachedObject(cached);
                }
                return cached;
            }
        }

        private class Definition<T> : Definition, IDefinition<T> 
            where T : class
        {
            public Definition(Func<T> createProc, CacheLifetime cacheLifetime)
                : base(createProc, cacheLifetime)
            {
                
            }

            public new T Get()
            {
                return (T)base.Get();
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

            private Func<T> CreateFunc<T>()
            {
                var t = typeof (T);
                var constructor = t.GetConstructors()
                    .Select(c => new {Constructor = c, Parameters = c.GetParameters()})
                    .OrderByDescending(c => c.Parameters.Length).First();

                var containerType = _container.GetType();
                var method = containerType.GetMethod("Get");

                var func = (Func<T>)Expression.Lambda(
                    Expression.GetFuncType(t),
                    Expression.New(constructor.Constructor,
                        constructor.Parameters.Select(
                            p =>
                                Expression.Call(Expression.Constant(_container, typeof (Container)),
                                    method.MakeGenericMethod(p.ParameterType))))).Compile();

                return func;
            }

            private Func<object> CreateFunc(Type t)
            {
                
                var constructor = t.GetConstructors()
                    .Select(c => new { Constructor = c, Parameters = c.GetParameters() })
                    .OrderByDescending(c => c.Parameters.Length).First();

                var containerType = _container.GetType();
                var method = containerType.GetMethod("Get", new Type[] {});

                var func = (Func<object>)Expression.Lambda(
                    Expression.GetFuncType(_defineFor),
                    Expression.New(constructor.Constructor,
                        constructor.Parameters.Select(
                            p =>
                                Expression.Call(Expression.Constant(_container, typeof(Container)),
                                    method.MakeGenericMethod(p.ParameterType))))).Compile();

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
                        return new ThreadCacheLifetime(_container);
                    default:
                        throw new ArgumentException("Lifetime must be one of the defined values.");
                }
            }

            public void Use(Type type, Lifetime cacheLifetime = Lifetime.None)
            {
                _container.typeMap.AddOrUpdate(_defineFor, t => (Definition)Activator.CreateInstance(typeof(Definition<>).MakeGenericType(_defineFor), CreateFunc(type), CreateLifetime(cacheLifetime)), (a, b) => { throw new Exception(); });
            }

            public void Use<T>(Lifetime cacheLifetime = Lifetime.None)
                where T : class
            {
                Use(typeof(T), cacheLifetime);//_container.typeMap.AddOrUpdate(_defineFor, t => new Definition<T>(CreateFunc<T>(), CreateLifetime(cacheLifetime)), (a, b) => { throw new Exception(); });
            }

            public void Use<T>(Func<T> proc, Lifetime cacheLifetime = Lifetime.None)
                where T : class
            {
                _container.typeMap.AddOrUpdate(_defineFor, t => new Definition<T>(proc, CreateLifetime(cacheLifetime)), (a, b) => new Definition<T>(proc, CreateLifetime(cacheLifetime)));
            }
        }

        public object GetService(Type serviceType)
        {
            Definition def;
            if (typeMap.TryGetValue(serviceType, out def))
            {
                return def.Get();
            }
            if (_parent != null)
                return _parent.GetService(serviceType);
            return null;
        }

        public object Get(string name)
        {
            var v = typeMap.FirstOrDefault(x => string.Equals(x.Key.Name, name, StringComparison.OrdinalIgnoreCase));
            if (v.Value != null)
            {
                return v.Value.Get();
            }

            if (_parent != null)
                return _parent.Get(name);

            throw new TypeDefinitionNotFoundException(null, "Could not locate a type defined with the name '" + name + "'.");
        }

        public object TryGet(string name)
        {
            var v = typeMap.FirstOrDefault(x => string.Equals(x.Key.Name, name, StringComparison.OrdinalIgnoreCase));
            if (v.Value != null)
            {
                return v.Value.Get();
            }

            if (_parent != null)
                return _parent.TryGet(name);

            return null;
        }

        public T Get<T>() where T : class
        {
            Definition def;
            if (typeMap.TryGetValue(typeof (T), out def))
            {
                return ((Definition<T>) def).Get();
            }
            if (_parent != null)
                return _parent.Get<T>();
            throw new TypeDefinitionNotFoundException(typeof(T));
        }

        public T TryGet<T>()
            where T : class
        {
            Definition def;
            if(typeMap.TryGetValue(typeof(T), out def))
            {
                return ((Definition<T>) def).Get();
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
