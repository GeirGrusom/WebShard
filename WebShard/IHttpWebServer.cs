using System;

namespace WebShard
{
    public interface IHttpWebServer
    {
        IHttpApplication Application { get; }
        event EventHandler<IHttpRequestContext> BeginRequest; 
        event EventHandler<RequestExceptionEventArgs> RequestException;
        event EventHandler<IHttpRequestContext> EndRequest;
    }
}