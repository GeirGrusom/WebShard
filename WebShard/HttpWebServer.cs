using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebShard
{
    public class HttpWebServer : IHttpWebServer
    {
        private readonly IHttpApplication _application;
        private readonly List<Listener> _listeners;
        private readonly CancellationTokenSource _cancel;

        public IHttpApplication Application { get { return _application; } }

        public HttpWebServer(IHttpApplication application, int httpPort = 80, int httpsPort = 443, WebServerFlags startupFlags = WebServerFlags.HttpOverIpv4 | WebServerFlags.HttpOverIpv6)
        {
            _application = application;
            _cancel = new CancellationTokenSource();
            _listeners = new List<Listener>();

            if ((WebServerFlags.HttpOverIpv4) == (startupFlags & (WebServerFlags.HttpOverIpv4)))
                _listeners.Add(new Ip4Listener(httpPort, false, this, _cancel.Token));

            if ((WebServerFlags.HttpsOverIpv4) == (startupFlags & (WebServerFlags.HttpsOverIpv4)))
                _listeners.Add(new Ip4Listener(httpsPort, true, this, _cancel.Token));

            if ((WebServerFlags.HttpOverIpv6) == (startupFlags & (WebServerFlags.HttpOverIpv6)))
                _listeners.Add(new Ip6Listener(httpPort, false, this, _cancel.Token));

            if ((WebServerFlags.HttpsOverIpv6) == (startupFlags & (WebServerFlags.HttpsOverIpv6)))
                _listeners.Add(new Ip6Listener(httpsPort, true, this, _cancel.Token));
        }

        public void Start()
        {
            Task[] listenTasks = _listeners.Select(x => new Task(x.Listen, _cancel.Token)).ToArray();
            foreach(var task in listenTasks)
                task.Start();
            Task.WaitAll(listenTasks, _cancel.Token);

        }

        private void OnBeginRequest(IHttpRequestContext request)
        {
            if (BeginRequest != null)
                BeginRequest(this, request);
        }

        private void OnEndRequest(IHttpRequestContext request)
        {
            if (EndRequest != null)
                EndRequest(this, request);
        }

        private void OnException(IHttpRequestContext context, Exception ex)
        {
            if(RequestException != null)
                RequestException(this, new RequestExceptionEventArgs(context, ex));
        }

        public event EventHandler<IHttpRequestContext> BeginRequest;
        public event EventHandler<RequestExceptionEventArgs> RequestException;
        public event EventHandler<IHttpRequestContext> EndRequest;
    }
}