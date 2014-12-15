using System;
using System.Collections.Generic;

namespace WebShard.Serialization.Json
{
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
}