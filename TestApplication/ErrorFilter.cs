using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebShard;
using WebShard.Filtering;

namespace TestApplication
{
    public class ErrorFilter : IExceptionFilter
    {
        public IResponse Process(IHttpRequestContext request, IHttpResponseContext response, Exception ex)
        {
            IResponse statusResponse;
            var httpException = (ex as HttpException);
            if (httpException != null)
                statusResponse = new StatusResponse(new Status(httpException.StatusCode, httpException.Message));
            else
                statusResponse = StatusResponse.InternalServerError;

            return new CompositeResponse(TestController.RenderException(ex), statusResponse);
        }
    }
}
