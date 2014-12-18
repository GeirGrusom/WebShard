using System.Collections.Generic;

namespace WebShard.Serialization.Json
{
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
            else if (token.Type == TokenType.Null)
                result = null;
            else
                throw new JsonDeserializationException(token);
            tokenStream.MoveNext();

            return result;
        }
    }
}