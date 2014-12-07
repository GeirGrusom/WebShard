using System.Collections.Generic;
using System.Linq;

namespace WebShard
{
    public class CompositeResponse : IResponse
    {
        private readonly IResponse[] _results;

        public CompositeResponse(params IResponse[] results)
        {
            _results = results;
        }

        public CompositeResponse(IEnumerable<IResponse> results)
            : this(results as IResponse[] ?? results.ToArray())
        {
        }

        public void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            foreach (var item in _results)
            {
                item.Write(request, context);
            }
        }
    }
}