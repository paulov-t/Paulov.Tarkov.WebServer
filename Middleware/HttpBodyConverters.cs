using ComponentAce.Compression.Libs.zlib;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIT.WebServer.Middleware
{
    public static class HttpBodyConverters
    {
        public static async Task<Dictionary<string, object>> DecompressRequestBodyToDictionary(HttpRequest request)
        {
            using var stream = request.Body;
            using ZLibStream zLibStream = new ZLibStream(stream, CompressionMode.Decompress);
            byte[] buffer = new byte[4096];
            await zLibStream.ReadAsync(buffer, 0, buffer.Length);
            var str = Encoding.UTF8.GetString(buffer);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(str);
        }

        public static async Task CompressDictionaryIntoResponseBody(Dictionary<string, object> dictionary, HttpResponse response)
        {
            await CompressStringIntoResponseBody(JsonConvert.SerializeObject(dictionary), response);
        }

        public static async Task CompressStringIntoResponseBody(string stringToConvert, HttpResponse response)
        {
            response.Headers.Add("Content-Encoding", "deflate");
            response.Headers.Add("Accept-Encoding", "deflate");
            var bytes = new byte[8196];// SimpleZlib.CompressToBytes(stringToConvert, 6);
            Pooled9LevelZLib.CompressToBytesNonAlloc(stringToConvert, bytes);
            var stream = new StreamWriter(new MemoryStream());
            stream.Write(bytes);
            stream.Flush();
            await response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(bytes));
        }

        public static async Task CompressDictionaryIntoResponseBodyBSG(Dictionary<string, object> dictionary, HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", dictionary);
            await CompressDictionaryIntoResponseBody(BSGResponse, response);
        }
    }
}
