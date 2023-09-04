using Microsoft.AspNetCore.RequestDecompression;

namespace SIT.WebServer.Providers
{
    public class ZLibDecompressionProvider : IDecompressionProvider
    {
        public Stream GetDecompressionStream(Stream stream)
        {
            // Write your code here to decompress
            return stream;
        }
    }
}
