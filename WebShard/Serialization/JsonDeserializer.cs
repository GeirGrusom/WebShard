using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    class JsonNumberDeserializer : IJsonInternalDeserializer
    {
        public static readonly JsonNumberDeserializer Instance = new JsonNumberDeserializer();
        public object Deserialize(ref IEnumerator<Token> tokenStream, Type resultType)
        {
            if(tokenStream.Current.Type != TokenType.Number)
                throw new FormatException("Expected number.");

            object result = Convert.ChangeType(tokenStream.Current.Value, resultType);
            tokenStream.MoveNext();
            return result;
        }
    }

    class JsonBoolDeserializer : IJsonInternalDeserializer
    {
        public static readonly JsonBoolDeserializer Instance = new JsonBoolDeserializer();


        public object Deserialize(ref IEnumerator<Token> tokenStream, Type resultType)
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

    class JsonArrayDeserializer : IJsonInternalDeserializer
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
            else
                throw new NotImplementedException();
            var values = (IList)Activator.CreateInstance(typeof (IList<>).MakeGenericType(arrayElements));

            tokenStream.MoveNext();
            while (tokenStream.Current.Type != TokenType.RightBracket)
            {
                object value = JsonDeserializer.Instance.Deserialize(ref tokenStream, arrayElements);
                values.Add(value);
                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
            }
            if (tokenStream.Current.Type != TokenType.RightBracket)
                throw new FormatException("Expected ']'");
            tokenStream.MoveNext();

            throw new NotImplementedException();
        }
    }

    public class JsonDeserializer
    {
        internal static readonly JsonDeserializer Instance = new JsonDeserializer();

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
                return JsonBoolDeserializer.Instance.Deserialize(ref enumerator, resultType);
            }
            if (token.Type == TokenType.LeftBrace && resultType.IsArray)
            {
                return JsonArrayDeserializer.Instance.Deserialize(ref enumerator, resultType);
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
            return Deserialize(ref enumerator, resultType);
        }
    }
}
