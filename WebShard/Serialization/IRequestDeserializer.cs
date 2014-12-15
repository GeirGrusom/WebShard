using System;
using System.IO;

namespace WebShard.Serialization
{
    public interface IRequestDeserializer
    {
        T Deserialize<T>(Stream input);
        object Deserialize(Stream input);
        object Deserialize(Stream input, Type resultType);
    }
}
