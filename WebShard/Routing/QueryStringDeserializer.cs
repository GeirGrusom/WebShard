using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebShard.Routing
{

    public static class QueryString
    {

        internal static readonly HashSet<Type> clrTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
            typeof(decimal), typeof(string)
        };

        public static bool TryDeserialize(string queryString, Type type, out object result)
        {
            if (clrTypes.Contains(type))
            {
                result = Convert.ChangeType(queryString, type);
                return true;
            }
            var qd = new QueryStringDeserializer();
            return qd.TryDeserializeInternal(queryString, type, out result);
        }

        public static bool TryDeserialize<T>(string queryString, out T result)
        {
            object res;
            bool status = TryDeserialize(queryString, typeof (T), out res);
            result = (T) res;
            return status;

        }

        private static readonly Regex KeyValueRegex = new Regex("(?<Key>[^&=]+)=(?<Value>[^&=]*)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static IDictionary<string, string> Parse(string queryString)
        {
            var m = KeyValueRegex.Matches(queryString.TrimStart('?')).Cast<Match>();
            return m.ToDictionary(k => WebUtility.UrlDecode(k.Groups["Key"].Value), k => WebUtility.UrlDecode(k.Groups["Value"].Value), StringComparer.OrdinalIgnoreCase);
        }
    }

    public class QueryStringDeserializer
    {
        public bool TryDeserializeInternal(string queryString, Type type, out object result)
        {
            var results = QueryString.Parse(queryString);

            var ctor = type.GetConstructor(new Type[0]);
            if(ctor == null)
                throw new NotImplementedException("Currently the type needs a public default constructor.");

            object res = Activator.CreateInstance(type);

            foreach (var key in results.Keys)
            {
                var propertyIndex = key.IndexOf('.');
                if (propertyIndex < 0)
                {
                    var property = type.GetProperty(key);
                    if(property == null || !property.CanWrite)
                        throw new SerializationException(string.Format("Type {0} does not have a public writable property named {1}.", type, key));

                    if (property.PropertyType == typeof (string))
                    {
                        property.SetValue(res, results[key]);
                        continue;
                    }
                    
                    // Value is empty and type is nullable (class or struct).

                    if (results[key] == "")
                    {
                        if(property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            property.SetValue(res, Activator.CreateInstance(property.PropertyType));
                        else if(property.PropertyType.IsClass)
                            property.SetValue(res, null);
                        continue;
                    }

                    // int, long, etc.
                    if (QueryString.clrTypes.Contains(property.PropertyType))
                    {
                        object value = Convert.ChangeType(results[key], property.PropertyType, CultureInfo.InvariantCulture);
                        property.SetValue(res, value);
                        continue;
                    }

                    // T?
                    if (property.PropertyType.IsGenericType &&
                        property.PropertyType.GetGenericTypeDefinition() == typeof (Nullable<>))
                    {
                        object value = Convert.ChangeType(results[key], property.PropertyType.GetGenericArguments()[0], CultureInfo.InvariantCulture);
                        property.SetValue(res, Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]), value));
                        continue;
                    }
                }
            }

            result = res;
            return true;
        }
    }
}
