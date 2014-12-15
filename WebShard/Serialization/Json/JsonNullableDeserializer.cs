using System.Collections.Generic;
using System.Reflection;

namespace WebShard.Serialization.Json
{
    static class JsonNullableDeserializer<T>
        where T : struct 
    {
        private static readonly DeserializeElement<T> DeserProc;

        static JsonNullableDeserializer()
        {
            var type = JsonDeserializer.GetDeserializer<T>();
            var method = type.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static, null,
                new[] {typeof (IEnumerator<Token>).MakeByRefType()}, null);
            DeserProc = (DeserializeElement<T>)method.CreateDelegate(typeof(DeserializeElement<T>));
        }

        public static T? Deserialize(ref IEnumerator<Token> tokenStream)
        {
            if (tokenStream.Current.Type == TokenType.Null)
            {
                tokenStream.MoveNext();
                return null;
            }
            return DeserProc(ref tokenStream);
        }
    }
}