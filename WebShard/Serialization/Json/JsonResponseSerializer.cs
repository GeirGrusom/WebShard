using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Serialization.Json
{
    public class JsonResponseSerializer : IResponseSerializer
    {
        private readonly JsonSerializer _serializer;
        public string ContentType { get { return "application/json"; } }
        public JsonResponseSerializer()
        {
            _serializer = new JsonSerializer();
        }

        public void Serialize(object input, Stream target)
        {
            byte[] data = Encoding.UTF8.GetBytes(_serializer.Serialize(input));
            target.Write(data, 0, data.Length);
        }

        public void Serialize<T>(T input, Stream target)
        {
            byte[] data = Encoding.UTF8.GetBytes(_serializer.Serialize(input));
            target.Write(data, 0, data.Length);
        }
    }
}
