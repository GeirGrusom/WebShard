using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace WebShard
{
    public sealed class ClientProcess
    {
        private readonly TcpClient _client;
        private readonly CancellationToken _cancelToken;
        private readonly bool _isSecure;
        private readonly IHttpWebServer _webServer;
        private readonly CountdownEvent _counter;

        public ClientProcess(TcpClient client, CountdownEvent counter, CancellationToken token, IHttpWebServer webServer, bool isSecure)
        {
            _client = client;
            _cancelToken = token;
            _isSecure = isSecure;
            _counter = counter;
            _webServer = webServer;
        }

        private Stream SecureStream(Stream input)
        {
            var ssl = new SslStream(input, false);
            ssl.AuthenticateAsServer(X509Certificate.CreateFromCertFile(@"..\..\cert.cert"), false, SslProtocols.Tls12, false);
            return ssl;
        }

        public void Read()
        {
            using (var netStream = _client.GetStream())
            {
                Stream stream;
                try
                {
                    stream = _isSecure ? SecureStream(netStream) : netStream;
                }
                catch (IOException)
                {
                    netStream.Dispose();
                    return;
                }

                while (_client.Connected && !_cancelToken.IsCancellationRequested)
                {
                    do
                    {
                        if(!_client.Connected || _cancelToken.IsCancellationRequested)
                            goto exitRead;

                        Thread.Sleep(0);
                    } while (!netStream.DataAvailable);

                    var request = HttpRequestContext.CreateFromStream(stream, _isSecure ? "https" : "http",
                        _client.Client.RemoteEndPoint.ToString());

                    var response = _webServer.Application.ProcessRequest(request);
                    response.WriteResponse(stream);
                    stream.Flush();
                    if (response.Headers.Connection != "keep-alive")
                        break;
                }
            }
        exitRead:  _counter.Signal();
        }
    }
}