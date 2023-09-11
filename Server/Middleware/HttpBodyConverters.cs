using ComponentAce.Compression.Libs.zlib;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO.Compression;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIT.WebServer.Middleware
{
    public static class HttpBodyConverters
    {
        public static async Task<Dictionary<string, object>> DecompressRequestBodyToDictionary(HttpRequest request)
        {
            if (!request.Body.CanSeek)
                request.EnableBuffering();

            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            request.Body.Position = 0;

            // This is the only way to handle Zlib versus Standard Json calls
            try
            {
                using ZLibStream zLibStream = new ZLibStream(request.Body, CompressionMode.Decompress);
                byte[] buffer = new byte[4096];
                await zLibStream.ReadAsync(buffer, 0, buffer.Length);
                var str = Encoding.UTF8.GetString(buffer);
                var resultDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(str);
                if (resultDict != null)
                    return resultDict;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            }
            catch
            {
                return null;
            }
          
        }

        public static async Task<(string, dynamic)> DecompressRequestBody(HttpRequest request)
        {
            if (!request.Body.CanSeek)
                request.EnableBuffering();

            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8);
            string resultString = await reader.ReadToEndAsync().ConfigureAwait(false);
            request.Body.Position = 0;

            dynamic resultDynamic = new ExpandoObject();

            // This is the only way to handle Zlib versus Standard Json calls
            try
            {
                using ZLibStream zLibStream = new ZLibStream(request.Body, CompressionMode.Decompress);
                byte[] buffer = new byte[4096];
                await zLibStream.ReadAsync(buffer, 0, buffer.Length);
                resultString = Encoding.UTF8.GetString(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                if (resultString.StartsWith("{"))
                    resultDynamic = JObject.Parse(resultString);
                else if (resultString.StartsWith("["))
                    resultDynamic = JArray.Parse(resultString);
            }
            catch
            {
            }

            return (resultString, resultDynamic);


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
            //if(!response.Headers.ContainsKey("Set-Cookie"))
            //    response.Headers.Add("Set-Cookie", $"PHPSESSID=");
            response.ContentType = "application/json";
            response.StatusCode = 200;

            if (!string.IsNullOrEmpty(stringToConvert))
            {
                if (request.Headers.AcceptEncoding == "deflate, gzip" || request.Headers.AcceptEncoding == "deflate")
                {
                    var bytes = new byte[(1024 * 1024) * 50];
                    Pooled9LevelZLib.CompressToBytesNonAlloc(stringToConvert, bytes);
                    //bytes = SimpleZlib.CompressToBytes(stringToConvert, 9);
                    //var stream = new StreamWriter(new MemoryStream());
                    //stream.Write(bytes);
                    //stream.Flush();

                    var rom = new ReadOnlyMemory<byte>(bytes);
                    response.Headers.ContentLength = rom.Length;
                    await response.BodyWriter.WriteAsync(rom);
                    bytes = null;
                    GC.Collect();
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

        public static async Task CompressIntoResponseBodyBSG(string data, HttpRequest request, HttpResponse response, int errorCode, string errorMessage)
        {
            var resp = "{ 'err': " + errorCode + ", 'errmsg': " + errorMessage + ", 'data': " + data + " }";
            await CompressStringIntoResponseBody(resp, request, response);
        }


        public static async Task CompressIntoResponseBodyBSG(string data, HttpRequest request, HttpResponse response)
        {
            data = SanitizeJson(data);
            var resp = "{ 'err': 0, 'errmsg':null, 'data': " + data + " }";
            await CompressStringIntoResponseBody(resp, request, response);
        }

        public static string SanitizeJson(string json)
        {
            return json.Replace("\r\n", "");
        }

        public static async Task CompressIntoResponseBodyBSG(Dictionary<string, object> dictionary, HttpRequest request, HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", dictionary);
            await CompressDictionaryIntoResponseBody(BSGResponse, request, response);
        }

        public static void CompressIntoResponseBodyBSG(Dictionary<string, object> dictionary, ref HttpRequest request, ref HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", dictionary);
            CompressDictionaryIntoResponseBody(BSGResponse, request, response).RunSynchronously();
        }

        public static async Task CompressIntoResponseBodyBSG(JObject obj, HttpRequest request, HttpResponse response)
        {
            Dictionary<string, object> BSGResponse = new Dictionary<string, object>();
            BSGResponse.Add("err", 0);
            BSGResponse.Add("errmsg", null);
            BSGResponse.Add("data", obj);
            await CompressDictionaryIntoResponseBody(BSGResponse, request, response);
        }

        public static async Task CompressDictionaryIntoResponseBodyBSG(Dictionary<string, object> dictionary, HttpRequest request, HttpResponse response)
        {
            await CompressIntoResponseBodyBSG(dictionary, request, response);
        }
    }
}
