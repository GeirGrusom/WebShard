using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace WebShard.Serialization.Json
{


    internal delegate void ParseArrayElement<T>(ref IEnumerator<Token> tokenStream, List<T> list);


    internal delegate TResult DeserializeElement<out TResult>(ref IEnumerator<Token> tokenStream);

    public class JsonDeserializer 
    {
        internal static readonly JsonDeserializer Instance = new JsonDeserializer();

        internal static Type GetDeserializer<T>()
        {
            return GetDeserializer(typeof(T));
        }

        internal static Type GetDeserializer(Type t)
        {
            if (t == typeof (bool))
                return typeof (JsonBoolDeserializer);
            if (t == typeof (string))
                return typeof (JsonStringDeserializer);

            if (t == typeof (DateTime))
                return typeof (JsonDateTimeDeserializer);

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof (Nullable<>))
                return typeof (JsonNullableDeserializer<>).MakeGenericType(t.GetGenericArguments()[0]);

            // Check if the type has a TryParse function.
            if (t.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider) }, null) != null)
                return typeof(JsonParseNumberStylesFormatProviderDeserializer<>).MakeGenericType(t);

            if (t.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
                    new[] {typeof (string), typeof (IFormatProvider)}, null) != null)
                return typeof (JsonParseFormatProviderDeserializer<>).MakeGenericType(t);

            if (t.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(string), }, null) != null)
                return typeof(JsonParseDeserializer<>).MakeGenericType(t);

            if (t.IsArray)
                return typeof (JsonArrayDeserializer<>).MakeGenericType(t.GetElementType());

            if (t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                // Deserialize IEnumerable<T> as T[].
                return GetDeserializer(t.GetGenericArguments()[0].MakeArrayType());

            if (t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IDictionary<,>))
            {
                // Deserialize IDictionary<,> as Dictionary<,>
                var args = t.GetGenericArguments();
                return
                    typeof (JsonDictionaryDeserializer<,,>).MakeGenericType(
                        typeof (Dictionary<,>).MakeGenericType(args[0], args[1]), args[0], args[1]);
            }

            if (t == typeof (object))
            {
                return typeof (JsonObjectDeserializer);
            }

            return typeof (JsonObjectDeserializer<>).MakeGenericType(t);

        }



        internal static T Deserialize<T>(ref IEnumerator<Token> tokenStream)
        {
            return (T)Deserialize(ref tokenStream, typeof (T));
        }

        internal static object Deserialize(ref IEnumerator<Token> tokenStream, Type resultType)
        {
            var type = GetDeserializer(resultType);

            var invokeMethod = type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(IEnumerator<Token>).MakeByRefType() }, null);

            return invokeMethod.Invoke(null, new object[] { tokenStream });
        }

        private static class TypeDeserializerCache<T>
        {
            private static readonly DeserializeElement<T> DeserializeProc;

            static TypeDeserializerCache()
            {
                var type = GetDeserializer(typeof(T));

                var invokeMethod = type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);

                DeserializeProc = (DeserializeElement<T>)invokeMethod.CreateDelegate(typeof (DeserializeElement<T>));
            }

            public static T Deserialize(string json)
            {
                var tokens = new JsonTokenizer(json);
                var enumerator = tokens.Tokenize()
                        .Where(t => t.Type != TokenType.Whitespace && t.Type != TokenType.Comment)
                        .GetEnumerator();
                if (!enumerator.MoveNext())
                    return default(T);
                return DeserializeProc(ref enumerator);
            }
        }

        public T Deserialize<T>(string json)
        {
            return TypeDeserializerCache<T>.Deserialize(json);
        }

        public object Deserialize(string json, Type resultType)
        {
            if(json == null)
                throw new ArgumentNullException("json");
            if (resultType == null)
                throw new ArgumentNullException("resultType");

            var tokens = new JsonTokenizer(json);
            var enumerator = tokens.Where(t => t.Type != TokenType.Whitespace && t.Type != TokenType.Comment).GetEnumerator();
            if (!enumerator.MoveNext())
                return null;

            var type = GetDeserializer(resultType);

            var invokeMethod = type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);

            return invokeMethod.Invoke(null, new object[] {enumerator});
        }

        public object Deserialize(string json)
        {
            return Deserialize<object>(json);
        }
    }
}
