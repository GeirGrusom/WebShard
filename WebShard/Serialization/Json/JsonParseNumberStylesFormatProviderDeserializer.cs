using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace WebShard.Serialization.Json
{
    internal static class JsonParseNumberStylesFormatProviderDeserializer<T>
    {
        private static readonly Func<string, T> Parse;


        static JsonParseNumberStylesFormatProviderDeserializer()
        {
            var input = Expression.Parameter(typeof(string), "input");
            var tryParse = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
                new[] {typeof (string), typeof(NumberStyles), typeof (IFormatProvider)}, null);
            var lambda = Expression.Lambda<Func<string, T>>(
                Expression.Call(null, tryParse, input, Expression.Constant(NumberStyles.Number), Expression.Constant(CultureInfo.InvariantCulture)), input
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