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

        public ClientProcess(TcpClient client, CancellationToken token, IHttpWebServer webServer, bool isSecure)
        {
            _client = client;
            _cancelToken = token;
            _isSecure = isSecure;
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

                    while (!netStream.DataAvailable)
                    {
                        Thread.Sleep(0);
                    }


                    var request = HttpRequestContext.CreateFromStream(stream, _isSecure ? "https" : "http",
                        _client.Client.RemoteEndPoint.ToString());
                    /*
                    var response = new HttpResponseContext(request);
                    if (request.Headers.Connection == "keep-alive")
                        response.Headers.Connection = "keep-alive";
                    else
                        response.Headers.Connection = "close";

                    var content =
                        new ContentResponse(
                            "<!doctype html><html><heade><title>Test</title></head><body><h4>Hello World!</h4></body></html>");
                    content.Write(response);
                     */
                    var response = _webServer.Application.ProcessRequest(request);
                    response.WriteResponse(stream);
                    stream.Flush();
                    if (request.Headers.Connection != "keep-alive")
                        break;
                }
            }
        }
    }
}