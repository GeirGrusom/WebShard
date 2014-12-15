using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using WebShard.Ioc;

namespace WebShard.Serialization.Json
{
    static class JsonObjectDeserializer<T>
    {
        private static readonly DeserializeElement<T> deserProc; 

        static JsonObjectDeserializer()
        {
            var ctors = typeof(T).GetConstructors();
            var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (ctor == null)
                throw new TypeConstructionException(typeof(T), "The type does not have any public properties.");

            if (ctor.GetParameters().Length == 0)
                throw new TypeConstructionException(typeof(T), "Only a public default constructor is provided, which is not yet supported.");

        }

        // TODO: Write compiled version
        private static T DeserializePropertySets(ref IEnumerator<Token> tokenStream)
        {
            var result = Activator.CreateInstance<T>();
            Dictionary<string, PropertyInfo> properties =
                typeof (T).GetProperties()
                    .Where(p => p.CanWrite)
                    .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            while (tokenStream.Current.Type != TokenType.RightBrace)
            {
                var nameToken = tokenStream.Current;
                var left = JsonStringDeserializer.Deserialize(ref tokenStream);
                PropertyInfo par;
                if (!properties.TryGetValue(left, out par))
                    throw new JsonDeserializationException(nameToken, "Missing field");
                if (tokenStream.Current.Type != TokenType.Colon)
                    throw new JsonDeserializationException(tokenStream.Current, "Expected ':'");
                tokenStream.MoveNext();

                var right = JsonDeserializer.Deserialize(ref tokenStream, par.PropertyType);
                par.SetValue(result, right);

                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }

            if (tokenStream.Current.Type != TokenType.RightBrace)
                throw new JsonDeserializationException(tokenStream.Current, "Expected '}'");

            return result;
        }

        // TODO: Write a compiled version.
        public static T Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;
            if (token.Type == TokenType.Null)
            {
                tokenStream.MoveNext();
                return default(T);
            }

            if (token.Type != TokenType.LeftBrace)
                throw new FormatException();

            var ctors = typeof (T).GetConstructors();

            var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if(ctor == null)
                throw new JsonDeserializationException(token, string.Format("Unable to deserialize '{0}' because it has no public constructors.", typeof(T)));

            if (ctor.GetParameters().Length == 0)
                return DeserializePropertySets(ref tokenStream);

            tokenStream.MoveNext();

            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var parameters = ctor.GetParameters().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            while (tokenStream.Current.Type != TokenType.RightBrace)
            {
                var nameToken = tokenStream.Current;
                var left = JsonStringDeserializer.Deserialize(ref tokenStream);
                ParameterInfo par;
                if(!parameters.TryGetValue(left, out par))
                    throw new JsonDeserializationException(nameToken, "Missing field");
                if(tokenStream.Current.Type != TokenType.Colon)
                    throw new JsonDeserializationException(tokenStream.Current, "Expected ':'");
                tokenStream.MoveNext();

                var right = JsonDeserializer.Deserialize(ref tokenStream, par.ParameterType);
                values[left] = right;

                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }
            
            if(tokenStream.Current.Type != TokenType.RightBrace)
                throw new JsonDeserializationException(tokenStream.Current, "Expected '}'");

            foreach (var item in parameters.Keys.Where(p => !values.ContainsKey(p)))
            {
                var p = parameters[item];
                if(!p.HasDefaultValue)
                    throw new JsonDeserializationException(token, "The parameter '{0}' is not defined and has no default value.");
                values[item] = p.DefaultValue;
            }

            return (T) ctor.Invoke(parameters.Values.OrderBy(p => p.Position).Select(p => values[p.Name]).ToArray());

        }
    }

    static class JsonObjectDeserializer
    {
        public static object Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var token = tokenStream.Current;
            if (token.Type == TokenType.LeftBrace)
                return JsonDictionaryDeserializer<ExpandoObject, string, object>.Deserialize(ref tokenStream);
            if (token.Type == TokenType.String || token.Type == TokenType.Identifier)
                return JsonStringDeserializer.Deserialize(ref tokenStream);
            if (token.Type == TokenType.LeftBracket)
                return JsonArrayDeserializer<object>.Deserialize(ref tokenStream);
            if (token.Type == TokenType.Null)
            {
                tokenStream.MoveNext();
                return null;
            }
            if (token.Type == TokenType.Number)
                return JsonParseNumberStylesFormatProviderDeserializer<decimal>.Deserialize(ref tokenStream);
            if (token.Type == TokenType.False || token.Type == TokenType.True)
                return JsonBoolDeserializer.Deserialize(ref tokenStream);
            
            throw new JsonDeserializationException(token, string.Format("Unexpected '{0}' at this point.", token.Value));
        }
    }
}