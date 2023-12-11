using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SIT.WebServer.Middleware;

namespace SIT.Arena
{
    public class ArenaController : Controller
    {
        public ArenaController()
        {

        }

        [Route("/client/leaderboard")]
        [HttpGet]
        [HttpPost]
        public async void ClientLeaderboard(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(""), Request, Response);
        }

        [Route("client/arena/server/list")]
        [HttpPost]
        public async void ArenaServerList(
           [FromQuery] int? retry
       , [FromQuery] bool? debug
          )
        {
            // -------------------------------
            // ServerItem[]

            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            var result = Array.Empty<object>();

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(result), Request, Response);
        }

        [Route("client/arena/presets")]
        [HttpPost]
        public async void ArenaPresets(
          [FromQuery] int? retry
      , [FromQuery] bool? debug
         )
        {
            // -------------------------------
            // ArenaPresetsResponse

            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            var result = new Dictionary<string, object>();
            result.Add("presets", Array.Empty<object>());
            result.Add("presetTypes", Array.Empty<object>());

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(result), Request, Response);
        }
    }
}
