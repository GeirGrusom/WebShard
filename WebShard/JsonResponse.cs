using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WebShard.Serialization;
using WebShard.Serialization.Json;

namespace WebShard
{
    public sealed class JsonResponse : JsonResponse<object>
    {
        public JsonResponse(object model)
            : base(model)
        {
        }
    }

    public class JsonResponse<T> : ObjectResponse<T>
    {
        public JsonResponse(T model)
            : base(model, new JsonResponseSerializer())
        {
        }
    }
}
