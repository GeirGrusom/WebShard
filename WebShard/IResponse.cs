namespace WebShard
{
    public interface IResponse
    {
        void Write(IHttpRequestContext request, IHttpResponseContext context);
    }
}