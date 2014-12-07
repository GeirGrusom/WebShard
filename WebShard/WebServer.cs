using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WebShard
{
    public class Ip4Listener : Listener
    {
        public Ip4Listener(int port, bool isSecure, IHttpWebServer server, CancellationToken cancel)
            : base(port, isSecure, server, cancel, IPAddress.Any)
        {
        }
    }

    public class Ip6Listener : Listener
    {
        public Ip6Listener(int port, bool isSecure, IHttpWebServer server, CancellationToken cancel)
            : base(port, isSecure, server, cancel, IPAddress.IPv6Any)
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

        public int Port { get { return _port; } }
        public bool IsSecure { get { return _isSecure; } }

        protected Listener(int port, bool isSecure, IHttpWebServer webServer, CancellationToken cancel, IPAddress listenAddress)
        {
            _port = port;
            _listenAddress = listenAddress;
            _cancel = cancel;
            _isSecure = isSecure;
            _webServer = webServer;
        }

        public void Listen()
        {
            var listener = new TcpListener(_listenAddress, _port);
            listener.Start();
            while (!_cancel.IsCancellationRequested)
            {
                var client = listener.AcceptTcpClient();

                var process = new ClientProcess(client, _cancel, _webServer, _isSecure);

                var thread = new Thread(process.Read);
                thread.Start();
            }
            
        }
    }
}
