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
        private readonly CountdownEvent _counter;

        public IHttpApplication Application { get { return _application; } }

        public HttpWebServer(IHttpApplication application, int httpPort = 80, int httpsPort = 443, WebServerFlags startupFlags = WebServerFlags.HttpOverIpv4 | WebServerFlags.HttpOverIpv6)
        {
            _counter = new CountdownEvent(0);
            _application = application;
            _cancel = new CancellationTokenSource();
            _listeners = new List<Listener>();

            if ((WebServerFlags.HttpOverIpv4) == (startupFlags & (WebServerFlags.HttpOverIpv4)))
                _listeners.Add(new Ip4Listener(httpPort, false, this, _counter, _cancel.Token));

            if ((WebServerFlags.HttpsOverIpv4) == (startupFlags & (WebServerFlags.HttpsOverIpv4)))
                _listeners.Add(new Ip4Listener(httpsPort, true, this, _counter, _cancel.Token));

            if ((WebServerFlags.HttpOverIpv6) == (startupFlags & (WebServerFlags.HttpOverIpv6)))
                _listeners.Add(new Ip6Listener(httpPort, false, this, _counter, _cancel.Token));

            if ((WebServerFlags.HttpsOverIpv6) == (startupFlags & (WebServerFlags.HttpsOverIpv6)))
                _listeners.Add(new Ip6Listener(httpsPort, true, this, _counter, _cancel.Token));
        }

        public void Start()
        {
            foreach (var listener in _listeners)
            {
                listener.Start();
            }
            while (!_cancel.IsCancellationRequested)
            {
                foreach (var listener in _listeners)
                {
                    listener.Listen();
                }
                Thread.Sleep(10);
            }
        }

        public void Stop()
        {
            _cancel.Cancel();
            _counter.Wait();
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