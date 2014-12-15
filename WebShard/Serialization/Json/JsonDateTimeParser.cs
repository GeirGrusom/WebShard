using System;

namespace WebShard.Serialization.Json
{
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
}