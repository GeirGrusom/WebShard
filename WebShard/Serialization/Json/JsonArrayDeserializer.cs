using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace WebShard.Serialization.Json
{
    static class JsonArrayDeserializer<T>// : IJsonInternalDeserializer<T[]>, IJsonInternalDeserializer
    {

        private static readonly ParseArrayElement<T> ParseElement;

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
            ParseElement = lambda.Compile();
        }

        public static T[] Deserialize(ref IEnumerator<Token> tokenStream)
        {
            if(tokenStream.Current.Type != TokenType.LeftBracket)
                throw new FormatException("Expected '['");

            var list = new List<T>();

            tokenStream.MoveNext();
            while (tokenStream.Current.Type != TokenType.RightBracket)
            {
                ParseElement(ref tokenStream, list);
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
}