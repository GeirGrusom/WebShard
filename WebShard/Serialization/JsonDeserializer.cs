using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace WebShard.Serialization
{
    public enum TokenizerState
    {
        
    }

    public struct Token
    {
        private readonly int _line;
        private readonly int _column;
        private readonly string _value;
        private readonly TokenType _type;
        public Token(int line, int column, string value, TokenType type)
        {
            _line = line;
            _column = column;
            _value = value;
            _type = type;
        }

        public int Line { get { return _line; } }
        public int Column { get { return _column; } }
        public string Value { get { return _value; } }
        public TokenType Type { get { return _type; } }

        public override string ToString()
        {
            return _value;
        }
    }

    public enum TokenType
    {
        Whitespace,
        Comment,
        Colon,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Identifier,
        String,
        Number,
        False,
        True,
        Null,
        Comma,
    }

    interface IJsonInternalDeserializer
    {
        object Deserialize(ref IEnumerator<Token> tokenStream, Type resultType);
    }

    interface IJsonInternalDeserializer<out T>
    {
        T Deserialize(ref IEnumerator<Token> tokenStream);
    }

    class JsonNumberDeserializer : IJsonInternalDeserializer
    {
        static class JsonNumberParser<T>
        {
            private static readonly Func<string, T> parse; 
            static JsonNumberParser()
            {
                var parseMethod = typeof (T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof (string), typeof (IFormatProvider)}, null);
                var input = Expression.Parameter(typeof (string));
                if (parseMethod == null)
                {
                    parseMethod = typeof (T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof (string)}, null);


                    parse = Expression.Lambda<Func<string, T>>(Expression.Call(parseMethod, input)).Compile();
                    return;
                }

                var cultureInfo = Expression.Constant(CultureInfo.InvariantCulture, typeof (IFormatProvider));
                parse = Expression.Lambda<Func<string, T>>(Expression.Call(parseMethod, input, cultureInfo)).Compile();
            }
            public static T Parse(string input)
            {
                return parse(input);
            }
        }

        public static readonly JsonNumberDeserializer Instance = new JsonNumberDeserializer();
        public object Deserialize(ref IEnumerator<Token> tokenStream, Type resultType)
        {
            if(tokenStream.Current.Type != TokenType.Number)
                throw new FormatException("Expected number.");

            object result = Convert.ChangeType(tokenStream.Current.Value, resultType);
            tokenStream.MoveNext();
            return result;
        }

        public T Deserialize<T>(ref IEnumerator<Token> tokenStream)
        {
            return JsonNumberParser<T>.Parse(tokenStream.Current.Value);
        }
    }

    class JsonBoolDeserializer
    {
        internal static readonly JsonBoolDeserializer Instance = new JsonBoolDeserializer();

        public bool Deserialize(ref IEnumerator<Token> tokenStream)
        {
            bool result;
            if (tokenStream.Current.Type == TokenType.True)
                result = true;
            else if (tokenStream.Current.Type == TokenType.False)
                result = false;
            else
                throw new InvalidOperationException("Boolean deserializer doesn't understand non-bool.");
            tokenStream.MoveNext();
            return result;
        }
    }

    static class TypeHelper
    {
        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            return type.GetInterface(typeof(TInterface).Name) != null; 
        }
        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            return type.GetInterface(interfaceType.Name) != null;
        }

    }

    internal delegate void ParseArrayElement<T>(ref IEnumerator<Token> tokenStream, List<T> list);

    class JsonArrayDeserializer<T>// : IJsonInternalDeserializer<T[]>, IJsonInternalDeserializer
    {

        internal static readonly JsonArrayDeserializer<T> Instance = new JsonArrayDeserializer<T>();

        private static readonly ParseArrayElement<T> parseElement;

        static JsonArrayDeserializer()
        {
            var deserializerType = JsonDeserializer.GetDeserializer<T>();
            var input = Expression.Parameter(typeof (IEnumerator<Token>).MakeByRefType(), "tokenStream");
            var output = Expression.Parameter(typeof (ICollection<T>));
            var addProc = typeof (ICollection<T>).GetMethod("Add", new [] { typeof (T)});
            var func = deserializerType.GetMethod("Deserialize", new[] {typeof (IEnumerator<Token>).MakeByRefType()});
            object instance =
                deserializerType.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var lambda =
                Expression.Lambda<ParseArrayElement<T>>(
                    Expression.Call(output, addProc,
                        Expression.Call(Expression.Constant(instance, deserializerType), func, input)), input, output);
            parseElement = lambda.Compile();
        }

        public T[] Deserialize(ref IEnumerator<Token> tokenStream)
        {
            if(tokenStream.Current.Type != TokenType.LeftBracket)
                throw new FormatException("Expected '['");

            var list = new List<T>();

            tokenStream.MoveNext();
            while (tokenStream.Current.Type != TokenType.RightBracket)
            {
                parseElement(ref tokenStream, list);
                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }
            if (tokenStream.Current.Type != TokenType.RightBracket)
                throw new FormatException("Expected ']'");
            tokenStream.MoveNext();
            return list.ToArray();
            throw new NotImplementedException();
        }
    }



    /*class JsonArrayDeserializer : IJsonInternalDeserializer
    {

        public static readonly JsonArrayDeserializer Instance = new JsonArrayDeserializer();
        public object Deserialize(ref IEnumerator<Token> tokenStream, Type resultType)
        {
            if (tokenStream.Current.Type != TokenType.LeftBracket)
            {
                throw new FormatException("Expected '['");
            }

            Type arrayElements;
            if (resultType.IsArray)
            {
                arrayElements = resultType.GetElementType();
            }
            else if(resultType.ImplementsInterface(typeof(IList<>)))
            {
                
            }
            var values = (IList)Activator.CreateInstance(typeof (List<>).MakeGenericType(arrayElements));

            tokenStream.MoveNext();
            while (tokenStream.Current.Type != TokenType.RightBracket)
            {
                object value = JsonDeserializer.Instance.Deserialize(ref tokenStream, arrayElements);
                values.Add(value);
                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }
            if (tokenStream.Current.Type != TokenType.RightBracket)
                throw new FormatException("Expected ']'");
            tokenStream.MoveNext();

            var m = typeof (System.Linq.Enumerable).GetMethod("ToArray").MakeGenericMethod(arrayElements);
            return m.Invoke(null, new [] {values});

            throw new NotImplementedException();
        }
    }*/

    static class JsonDateTimeParser<T> // T will always be DateTime.s
    {
        private static readonly Func<string, T> ParseDateTime;

        static JsonDateTimeParser()
        {
            if(typeof(T) != typeof(DateTime))
                throw new InvalidOperationException();

//            var parseMethod = typeof(System.DateTime).GetMethod("Parse", BindingFlags.Public | )

            //ParseDateTime = Expression.Lambda<Func<string, T>>(Express)
        }

        public static T Parse(string input)
        {
            throw new NotImplementedException();
            return ParseDateTime(input);
        }

    }

    internal class JsonParseNumberStylesFormatProviderDeserializer<T>
    {
        private static readonly Func<string, T> Parse;

        internal static JsonParseNumberStylesFormatProviderDeserializer<T> Instance = new JsonParseNumberStylesFormatProviderDeserializer<T>(); 

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

        public T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            T result = Parse(tokenStream.Current.Value);
            tokenStream.MoveNext();
            return result;
        }
    }

    internal class JsonParseFormatProviderDeserializer<T>
    {
        private static readonly Func<string, T> Parse;
        internal static readonly JsonParseFormatProviderDeserializer<T> Instance = new JsonParseFormatProviderDeserializer<T>(); 
        static JsonParseFormatProviderDeserializer()
        {
            var input = Expression.Parameter(typeof(string), "input");
            var tryParse = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(string), typeof(IFormatProvider) }, null);
            var lambda = Expression.Lambda<Func<string, T>>(
                Expression.Call(null, tryParse, input, Expression.Constant(CultureInfo.InvariantCulture)), input
                );
            Parse = lambda.Compile();

        }

        public T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            T result = Parse(tokenStream.Current.Value);
            tokenStream.MoveNext();
            return result;
        }
    }

    internal class JsonParseDeserializer<T>
    {
        private static readonly Func<string, T> Parse;
        internal static readonly JsonParseDeserializer<T> Instance = new JsonParseDeserializer<T>(); 
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

        public T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            T result = Parse(tokenStream.Current.Value);
            tokenStream.MoveNext();
            return result;
        }
    }

    internal class JsonStringDeserializer
    {
        internal static readonly JsonStringDeserializer Instance = new JsonStringDeserializer();

        public string Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;
            string result;
            if (token.Type == TokenType.String)
                result = token.Value.Substring(1, token.Value.Length - 2);
            else if (token.Type == TokenType.Identifier)
                result = token.Value;
            else
                throw new FormatException();
            tokenStream.MoveNext();

            return result;
        }
    }

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
            {
                return GetDeserializer(t.GetGenericArguments()[0].MakeArrayType());
            }

            throw new NotImplementedException();
            
        }

        internal object Deserialize(ref IEnumerator<Token> enumerator, Type resultType)
        {
            var token = enumerator.Current;
            if (token.Type == TokenType.Number)
                return JsonNumberDeserializer.Instance.Deserialize(ref enumerator, resultType);
            if (resultType == typeof (string))
            {
                if(token.Type != TokenType.String && token.Type != TokenType.Identifier)
                    throw new FormatException("Expected string or identifier.");
                string value = token.Value;
                if (token.Type == TokenType.String)
                    value = value.Trim('"');
                enumerator.MoveNext();
                return value;
            }
            if (resultType == typeof (bool))
            {
                return JsonBoolDeserializer.Instance.Deserialize(ref enumerator);
            }
            if (token.Type == TokenType.LeftBracket && resultType.IsArray)
            {
                var elType = resultType.GetElementType();
                var deserializerType = typeof (JsonArrayDeserializer<>).MakeGenericType(elType);
                object deser = deserializerType.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                var deserMethod = deserializerType.GetMethod("Deserialize",
                    new[] {typeof (IEnumerator<Token>).MakeByRefType()});
                return deserMethod.Invoke(deser, new object [] { enumerator });
            }
            throw new NotImplementedException();
        }

        public T Deserialize<T>(string json)
        {
            return (T)Deserialize(json, typeof (T));
        }

        public object Deserialize(string json, Type resultType)
        {
            var tokens = new JsonTokenizer(json);
            var enumerator = tokens.Where(t => t.Type != TokenType.Whitespace && t.Type != TokenType.Comment).GetEnumerator();
            if (!enumerator.MoveNext())
                return null;

            var type = GetDeserializer(resultType);
            var instance = type.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

            var invokeMethod = type.GetMethod("Deserialize", new[] {typeof (IEnumerator<Token>).MakeByRefType()});

            return invokeMethod.Invoke(instance, new object[] {enumerator});

        }
    }
}
