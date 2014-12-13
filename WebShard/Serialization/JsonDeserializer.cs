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


    static class JsonBoolDeserializer
    {
        public static bool Deserialize(ref IEnumerator<Token> tokenStream)
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

    internal delegate void ParseArrayElement<T>(ref IEnumerator<Token> tokenStream, List<T> list);

    static class JsonArrayDeserializer<T>// : IJsonInternalDeserializer<T[]>, IJsonInternalDeserializer
    {

        private static readonly ParseArrayElement<T> parseElement;

        static JsonArrayDeserializer()
        {
            var deserializerType = JsonDeserializer.GetDeserializer<T>();
            var input = Expression.Parameter(typeof (IEnumerator<Token>).MakeByRefType(), "tokenStream");
            var output = Expression.Parameter(typeof (ICollection<T>));
            var addProc = typeof (ICollection<T>).GetMethod("Add", new [] { typeof (T)});
            var func = deserializerType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static, null,  new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);
            var lambda =
                Expression.Lambda<ParseArrayElement<T>>(
                    Expression.Call(output, addProc,
                        Expression.Call(null, func, input)), input, output);
            parseElement = lambda.Compile();
        }

        public static T[] Deserialize(ref IEnumerator<Token> tokenStream)
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

    internal static class JsonParseFormatProviderDeserializer<T>
    {
        private static readonly Func<string, T> Parse;
        
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

        public static T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            T result = Parse(tokenStream.Current.Value);
            tokenStream.MoveNext();
            return result;
        }
    }

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

    internal static class JsonStringDeserializer
    {

        public static string Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;
            string result;
            if (token.Type == TokenType.String)
                result = token.Value.Substring(1, token.Value.Length - 2);
            else if (token.Type == TokenType.Identifier)
                result = token.Value;
            else
                throw new DeserializationException(token);
            tokenStream.MoveNext();

            return result;
        }
    }

    

    internal delegate TResult DeserializeElement<out TResult>(ref IEnumerator<Token> tokenStream);

    static class JsonDictionaryDeserializer<TCollection, TKey, TValue>
        where TCollection : IDictionary<TKey, TValue>
    {
        private static readonly DeserializeElement<TKey> deserializeKey;
        private static readonly DeserializeElement<TValue> deserializeValue; 

        static JsonDictionaryDeserializer()
        {
            deserializeKey = TypeHelper.CreateDeserializeProc<TKey>();
            deserializeValue = TypeHelper.CreateDeserializeProc<TValue>();

        }

        public static TCollection Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;

            if(token.Type != TokenType.LeftBrace)
                throw new DeserializationException(token, "Expected '{'");
            tokenStream.MoveNext();

            var result = Activator.CreateInstance<TCollection>();

            while (tokenStream.Current.Type != TokenType.RightBrace)
            {
                var key = deserializeKey(ref tokenStream);
                if(tokenStream.Current.Type != TokenType.Colon)
                    throw new DeserializationException(tokenStream.Current, "Expected ','");
                tokenStream.MoveNext();
                var value = deserializeValue(ref tokenStream);

                result.Add(key,value);

                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }
            if(tokenStream.Current.Type != TokenType.RightBrace)
                throw new DeserializationException(tokenStream.Current, "Expected '}'");
            tokenStream.MoveNext();

            return result;
        }
    }

    static class JsonObjectDeserializer<T>
    {

        //private static readonly Dictionary<string, Action<>> fi 

        public static T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;

            if (token.Type != TokenType.LeftBrace)
                throw new FormatException();

            var ctors = typeof (T).GetConstructors();

            var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if(ctor == null)
                throw new DeserializationException(token, string.Format("Unable to deserialize '{0}' because it has no public constructors.", typeof(T)));

            if (ctor.GetParameters().Length == 0)
                throw new DeserializationException(token, string.Format("The type '{0}' only has a public default constructor available. Not currently supported."));

            tokenStream.MoveNext();

            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var parameters = ctor.GetParameters().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            while (tokenStream.Current.Type != TokenType.RightBrace)
            {
                var nameToken = tokenStream.Current;
                var left = JsonStringDeserializer.Deserialize(ref tokenStream);
                ParameterInfo par;
                if(!parameters.TryGetValue(left, out par))
                    throw new DeserializationException(nameToken, "Missing field");
                if(tokenStream.Current.Type != TokenType.Colon)
                    throw new DeserializationException(tokenStream.Current, "Expected ':'");
                tokenStream.MoveNext();

                var right = JsonDeserializer.Deserialize(ref tokenStream, par.ParameterType);
                values[left] = right;

                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }
            
            if(tokenStream.Current.Type != TokenType.RightBrace)
                throw new DeserializationException(tokenStream.Current, "Expected '}'");

            foreach (var item in parameters.Keys.Where(p => !values.ContainsKey(p)))
            {
                var p = parameters[item];
                if(!p.HasDefaultValue)
                    throw new DeserializationException(token, "The parameter '{0}' is not defined and has no default value.");
                values[item] = p.DefaultValue;
            }

            return (T) ctor.Invoke(parameters.Values.OrderBy(p => p.Position).Select(p => values[p.Name]).ToArray());

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
/*        internal object Deserialize(ref IEnumerator<Token> enumerator, Type resultType)
        {
            var token = enumerator.Current;
            if (token.Type == TokenType.Number)
                return JsonNumberDeserializer.Deserialize(ref enumerator, resultType);
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
                
                var deserMethod = deserializerType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static, null,
                    new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);
                return deserMethod.Invoke(null, new object [] { enumerator });
            }
            throw new NotImplementedException();
        }*/

        

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
            
            var invokeMethod = type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);

            return invokeMethod.Invoke(null, new object[] {enumerator});
        }
    }
}
