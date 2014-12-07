using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Mvc
{
    [Flags]
    public enum HttpVerbs
    {
        Get = 1,
        Post = 1 << 1,
        Head = 1 << 2,
        Delete = 1 << 3,
        Patch = 1 << 4,
        Trace = 1 << 5
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HttpGetAttribute : AcceptVerb
    {
        public HttpGetAttribute()
            : base(HttpVerbs.Get)
        {
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public class AcceptVerb : Attribute
    {
        public readonly HttpVerbs Verbs;

        public AcceptVerb(HttpVerbs verbs)
        {
            Verbs = verbs;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    [Serializable]
    public sealed class RouteAttribute : Attribute
    {
        public string Url;

    }

    [Serializable]
    public sealed class DefaultActionAttribute : Attribute
    {
        
    }

    [Serializable]
    public sealed class ControllerAttribute : Attribute
    {
    }
}
