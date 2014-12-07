using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    public interface IHttpResponseContext
    {
        HeaderCollection Headers { get; }
        Status Status { get; set; }
        Stream Response { get; }
        void WriteResponse(Stream stream);
    }
}
