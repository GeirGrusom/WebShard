using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WebShard
{
    public class Ip4Listener : Listener
    {
        public Ip4Listener(int port, bool isSecure, IHttpWebServer server, CountdownEvent counter, CancellationToken cancel)
            : base(port, isSecure, server, counter, cancel, IPAddress.Any)
        {
        }
    }

    public class Ip6Listener : Listener
    {
        public Ip6Listener(int port, bool isSecure, IHttpWebServer server, CountdownEvent counter, CancellationToken cancel)
            : base(port, isSecure, server, counter, cancel, IPAddress.IPv6Any)
        {
        }
    }

    public abstract class Listener
    {
        private readonly int _port;
        private readonly IPAddress _listenAddress;
        private readonly bool _isSecure;
        private readonly IHttpWebServer _webServer;
        private readonly CancellationToken _cancel;
        private readonly CountdownEvent _counter;
        private readonly TcpListener _listener;

        public int Port { get { return _port; } }
        public bool IsSecure { get { return _isSecure; } }

        protected Listener(int port, bool isSecure, IHttpWebServer webServer, CountdownEvent counter, CancellationToken cancel, IPAddress listenAddress)
        {
            _port = port;
            _listenAddress = listenAddress;
            _cancel = cancel;
            _isSecure = isSecure;
            _webServer = webServer;
            _counter = counter;
            _listener = new TcpListener(_listenAddress, _port);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public void Start()
        {
            _listener.Start();
        }

        public void Listen()
        {
            if(_listener.Pending())
            {
                var client = _listener.AcceptTcpClient();

                var process = new ClientProcess(client, _counter, _cancel, _webServer, _isSecure);

                var thread = new Thread(process.Read);

                if(!_counter.TryAddCount())
                    _counter.Reset(1);
                thread.Start();
            }
        }
    }
}
