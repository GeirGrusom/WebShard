using System;
using System.Collections.Generic;
using System.Globalization;

namespace WebShard.Serialization.Json
{
    static class JsonDateTimeDeserializer
    {

        public static DateTime Deserialize(ref IEnumerator<Token> tokenStream)
        {
            var value = JsonStringDeserializer.Deserialize(ref tokenStream);

            return DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
        }

    }
}