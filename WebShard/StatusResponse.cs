namespace WebShard
{
    public class StatusResponse : IResponse
    {
        public static readonly StatusResponse NotFound = new StatusResponse(Status.NotFound);
        public static readonly StatusResponse BadRequest = new StatusResponse(Status.BadRequest);
        public static readonly StatusResponse Unauthorized = new StatusResponse(Status.Unauthorized);

        private readonly Status _status;
        public Status Status { get { return _status; } }

        public StatusResponse(Status status)
        {
            this._status = status;
        }

        public virtual void Write(IHttpRequestContext request, IHttpResponseContext context)
        {
            context.Status = _status;
        }
    }
}