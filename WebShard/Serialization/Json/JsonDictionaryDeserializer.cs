using System;
using System.Collections.Generic;

namespace WebShard.Serialization.Json
{
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
                throw new JsonDeserializationException(token, "Expected '{'");
            tokenStream.MoveNext();

            var result = Activator.CreateInstance<TCollection>();

            while (tokenStream.Current.Type != TokenType.RightBrace)
            {
                var key = deserializeKey(ref tokenStream);
                if(tokenStream.Current.Type != TokenType.Colon)
                    throw new JsonDeserializationException(tokenStream.Current, "Expected ','");
                tokenStream.MoveNext();
                var value = deserializeValue(ref tokenStream);

                result.Add(key,value);

                if (tokenStream.Current.Type != TokenType.Comma)
                    break;
                tokenStream.MoveNext();
            }
            if(tokenStream.Current.Type != TokenType.RightBrace)
                throw new JsonDeserializationException(tokenStream.Current, "Expected '}'");
            tokenStream.MoveNext();

            return result;
        }
    }
}