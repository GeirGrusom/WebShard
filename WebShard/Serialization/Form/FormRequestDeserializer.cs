using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Serialization.Form
{
    public class FormRequestDeserializer : IRequestDeserializer
    {
        private readonly FormDeserializer _deserializer;

        public FormRequestDeserializer()
        {
            _deserializer = new FormDeserializer();
        }

        public T Deserialize<T>(Stream input)
        {
            using (var sr = new StreamReader(input, Encoding.UTF8))
            {
                return _deserializer.Deserialize<T>(sr.ReadToEnd());
            }
        }

        public object Deserialize(Stream input)
        {
            using (var sr = new StreamReader(input, Encoding.UTF8))
            {
                return _deserializer.Deserialize(sr.ReadToEnd());
            }
        }

        public object Deserialize(Stream input, Type resultType)
        {
            using (var sr = new StreamReader(input, Encoding.UTF8))
            {
                return _deserializer.Deserialize(sr.ReadToEnd(), resultType);
            }
        }
    }
}
