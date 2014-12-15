using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Serialization
{

    public interface ISerializer
    {
        string Serialize(object input);
        string Serialize<T>(T input);
    }

    public interface IResponseSerializer
    {
        string ContentType { get; }
        void Serialize(object input, Stream target);
        void Serialize<T>(T input, Stream target);
    }

    internal sealed class ToStringSerializer : ISerializer
    {
        public static readonly ToStringSerializer Instance = new ToStringSerializer();
        public string Serialize(object input)
        {
            return input.ToString();
        }

        public string Serialize<T>(T input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class FloatSerializer : ISerializer
    {
        public static readonly FloatSerializer Instance = new FloatSerializer();
        public string Serialize(object input)
        {
            var value = (float) input;
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public string Serialize<T>(T input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class DoubleSerializer : ISerializer
    {
        public static readonly DoubleSerializer Instance = new DoubleSerializer();
        public string Serialize(object input)
        {
            var value = (double)input;
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public string Serialize<T>(T input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class BooleanSerializer : ISerializer
    {
        public static readonly BooleanSerializer Instance = new BooleanSerializer();

        public string Serialize(object input)
        {
            var value = (bool) input;
            return value.ToString().ToLower();
        }

        public string Serialize<T>(T input)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class StringSerializer : ISerializer
    {
        public static readonly StringSerializer Instance = new StringSerializer();

        public string Serialize(object input)
        {
            return "\"" + ((string) input).Replace("\"", "\\\"") + "\"";
        }

        public string Serialize<T>(T input)
        {
            return Serialize((object) input);
        }
    }

    public class EnumerableSerializer : ISerializer
    {
        public static readonly EnumerableSerializer Instance = new EnumerableSerializer();
        public string Serialize(object input)
        {
            var ie = (IEnumerable) input;
            return "[" + string.Join(", ", ie.Cast<object>().Select(JsonSerializer.Instance.Serialize)) + "]";
        }

        public string Serialize<T>(T input)
        {
            throw new NotSupportedException();
        }
    }

    public class DictionarySerializer : ISerializer
    {
        public static readonly DictionarySerializer Instance = new DictionarySerializer();
        public string Serialize(object input)
        {
            var ie = (IDictionary) input;

            var values = ie.Cast<dynamic>()
                .Select(x => JsonSerializer.Instance.Serialize(x.Key) + ": " + JsonSerializer.Instance.Serialize(x.Value));
            return "{" + string.Join(", ", values) + "}";
        }

        public string Serialize<T>(T input)
        {
            throw new NotSupportedException();
        }
    }

    public class ObjectSerializer : ISerializer
    {
        public static readonly ObjectSerializer Instance = new ObjectSerializer();
        private IEnumerable<KeyValuePair<string, object>> GetValues(object input, IEnumerable<PropertyInfo> properties)
        {
            return 
                from property in properties 
                let value = property.GetValue(input)
                select new KeyValuePair<string, object>(StringSerializer.Instance.Serialize(property.Name), JsonSerializer.Instance.Serialize(value));
        }

        public string Serialize(object input)
        {
            var properties = input.GetType().GetProperties();
            return "{" + string.Join(", ", GetValues(input, properties).Select(x => x.Key + ": " + x.Value)) + "}";
        }

        public string Serialize<T>(T input)
        {
            return Serialize((object) input);
        }
    }

    public class JsonSerializer : ISerializer
    {
        internal static readonly JsonSerializer Instance = new JsonSerializer();
        private static readonly Dictionary<Type, ISerializer> PrimitiveSerializers = new Dictionary<Type, ISerializer>
        {
            { typeof(bool), BooleanSerializer.Instance},
            { typeof(byte), ToStringSerializer.Instance},
            { typeof(char), ToStringSerializer.Instance},
            { typeof(short), ToStringSerializer.Instance},
            { typeof(ushort), ToStringSerializer.Instance},
            { typeof(int), ToStringSerializer.Instance},
            { typeof(uint), ToStringSerializer.Instance},
            { typeof(long), ToStringSerializer.Instance},
            { typeof(ulong), ToStringSerializer.Instance},
            { typeof(decimal), ToStringSerializer.Instance},
            { typeof(float), FloatSerializer.Instance},
            { typeof(double), DoubleSerializer.Instance},
            { typeof(string), StringSerializer.Instance}
        };
        
        public string Serialize(object input)
        {
            if (input == null)
                return "null";

            var type = input.GetType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                if ((bool) type.GetProperty("HasValue").GetValue(input, null))
                {
                    var value = type.GetProperty("Value").GetValue(input, null);
                    return Instance.Serialize(value);
                }
                return "null";
            }

            if (type.IsEnum)
                return ToStringSerializer.Instance.Serialize(input);

            ISerializer primitiveSerializer;
            if (PrimitiveSerializers.TryGetValue(type, out primitiveSerializer))
                return primitiveSerializer.Serialize(input);

            if (input is IDictionary)
                return DictionarySerializer.Instance.Serialize(input);

            if (input is IEnumerable)
                return EnumerableSerializer.Instance.Serialize(input);

            return ObjectSerializer.Instance.Serialize(input);
        }

        public string Serialize<T>(T input)
        {
            return Serialize((object)input);
        }
    }
}
