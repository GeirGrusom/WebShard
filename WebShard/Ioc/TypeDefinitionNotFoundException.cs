using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Ioc
{
    [Serializable]
    public class TypeDefinitionNotFoundException : Exception
    {
        private readonly Type _type;

        public Type Type { get { return _type; } }

        public TypeDefinitionNotFoundException(Type type, string message, Exception innerException)
            : base(message, innerException)
        {
            _type = type;
        }

        public TypeDefinitionNotFoundException(Type type, string message)
            : base(message)
        {
            _type = type;
        }

        public TypeDefinitionNotFoundException(Type type, Exception innerException)
            : this(type, "There is no definition of '" + type.Name + "' found in the container.", innerException)
        {
        }

        public TypeDefinitionNotFoundException(Type type)
            : this(type, "There is no definition of '" + type.Name + "' found in the container.")
        {
        }

        public TypeDefinitionNotFoundException(SerializationInfo info, StreamingContext context)
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
