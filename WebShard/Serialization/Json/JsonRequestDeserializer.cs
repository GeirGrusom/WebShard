using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Serialization.Json
{
    public class JsonRequestDeserializer : IRequestDeserializer
    {
        private readonly JsonDeserializer _deserializer;

        public JsonRequestDeserializer()
        {
            _deserializer = new JsonDeserializer();
        }

        public T Deserialize<T>(Stream input)
        {
            using (var streamReader = new StreamReader(input, Encoding.UTF8))
            {
                return _deserializer.Deserialize<T>(streamReader.ReadToEnd());
            }
        }

        public object Deserialize(Stream input)
        {
            using (var streamReader = new StreamReader(input, Encoding.UTF8))
            {
                return _deserializer.Deserialize(streamReader.ReadToEnd());
            }
        }

        public object Deserialize(Stream input, Type resultType)
        {
            using (var streamReader = new StreamReader(input, Encoding.UTF8))
            {
                return _deserializer.Deserialize(streamReader.ReadToEnd(), resultType);
            }
        }
    }
}
