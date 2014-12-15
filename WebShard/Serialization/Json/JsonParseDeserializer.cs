using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace WebShard.Serialization.Json
{
    internal static class JsonParseDeserializer<T>
    {
        private static readonly Func<string, T> Parse;
        
        static JsonParseDeserializer()
        {
            var input = Expression.Parameter(typeof(string), "input");
            var tryParse = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(string) }, null);
            var lambda = Expression.Lambda<Func<string, T>>(
                Expression.Call(null, tryParse, input), input
                );
            Parse = lambda.Compile();

        }

        public static T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            T result = Parse(tokenStream.Current.Value);
            tokenStream.MoveNext();
            return result;
        }
    }
}