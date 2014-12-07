using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Ioc
{
    [Serializable]
    public class TypeConstructionException : Exception
    {
        private readonly Type _type;

        public Type Type { get { return _type; } }

        public TypeConstructionException(Type type, string message, Exception innerException)
            : base(message, innerException)
        {
            _type = type;
        }

        public TypeConstructionException(Type type, string message)
            : base(message)
        {
            _type = type;
        }

        public TypeConstructionException(Type type, Exception innerException)
            : this(type, "There is no definition of '" + type.Name + "' found in the container.", innerException)
        {
        }

        public TypeConstructionException(Type type)
            : this(type, "There is no definition of '" + type.Name + "' found in the container.")
        {
        }

        public TypeConstructionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _type = (Type)info.GetValue("_type", typeof (Type));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_type", _type);
        }
    }
}
