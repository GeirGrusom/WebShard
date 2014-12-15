using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace WebShard.Serialization.Json
{
    static class TypeHelper
    {
        public static DeserializeElement<T> CreateDeserializeProc<T>()
        {
            var deType = JsonDeserializer.GetDeserializer<T>();
            var method = deType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static, null,
                new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);
            var input = Expression.Parameter(typeof (IEnumerator<Token>).MakeByRefType());

            var l = Expression.Lambda<DeserializeElement<T>>(
                Expression.Call(null, method, input), input
                );

            return l.Compile();

        }
        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            return type.GetInterface(typeof(TInterface).Name) != null; 
        }
        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            return type.GetInterface(interfaceType.Name) != null;
        }

    }
}