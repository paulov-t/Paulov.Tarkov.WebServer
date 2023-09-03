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

        public static async Task CompressDictionaryIntoResponseBody(Dictionary<string, object> dictionary, HttpRequest request, HttpResponse response)
        {
            await CompressStringIntoResponseBody(JsonConvert.SerializeObject(dictionary), request, response);
        }

        public static async Task CompressStringIntoResponseBody(string stringToConvert, HttpRequest request, HttpResponse response)
        {
            if (!string.IsNullOrEmpty(stringToConvert))
            {
                stringToConvert = stringToConvert.Replace("/[\b]/g", "");
                //.replace(/[\f] / g, "")
                //.replace(/[\n] / g, "")
                //.replace(/[\r] / g, "")
                //.replace(/[\t] / g, "")
                //.replace(/[\\] / g, "");
            }

            //response.Headers.Add("content-encoding", "deflate");
            //response.Headers.Add("accept-encoding", "deflate");
            response.Headers.Add("Content-Type", "application/json");
            //response.Headers.Add("Transfer-Encoding", "chunked");
            response.Headers.Add("Set-Cookie", $"PHPSESSID=sdasdasdasdasdasdsad");
            response.ContentType = "application/json";
            response.StatusCode = 200;

            if (!string.IsNullOrEmpty(stringToConvert))
            {
                if (request.Headers.AcceptEncoding == "deflate, gzip" || request.Headers.AcceptEncoding == "deflate")
                {
                    var bytes = new byte[8196];// SimpleZlib.CompressToBytes(stringToConvert, 6);
                    Pooled9LevelZLib.CompressToBytesNonAlloc(stringToConvert, bytes);
                    var stream = new StreamWriter(new MemoryStream());
                    stream.Write(bytes);
                    stream.Flush();
                    response.Headers.ContentLength = bytes.Length;
                    await response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(bytes));
                    //await response.WriteAsJsonAsync(stringToConvert);
                }
                else
                {
                    await response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(stringToConvert)));
                }
            }
        }

        public static async Task CompressNullIntoResponseBodyBSG(HttpRequest request, HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", null);
            await CompressDictionaryIntoResponseBody(BSGResponse, request, response);
        }

        public static async Task CompressIntoResponseBodyBSG(string data, HttpRequest request, HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", data);
            await CompressDictionaryIntoResponseBody(BSGResponse, request, response);
        }

        public static async Task CompressIntoResponseBodyBSG(Dictionary<string, object> dictionary, HttpRequest request, HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", dictionary);
            await CompressDictionaryIntoResponseBody(BSGResponse, request, response);
        }

        public static async Task CompressDictionaryIntoResponseBodyBSG(Dictionary<string, object> dictionary, HttpRequest request, HttpResponse response)
        {
            await CompressIntoResponseBodyBSG(dictionary, request, response);
        }
    }
}
