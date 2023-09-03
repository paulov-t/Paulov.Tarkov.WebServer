using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;

namespace SIT.WebServer.Controllers
{
    [ApiController]
    public class GameController : ControllerBase
    {
        private SaveProvider saveProvider { get; } = new SaveProvider();

        [Route("client/game/start", Name = "GameStart")]
        [HttpPost]
        public async void Start(int? retry, bool? debug)
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);

            await HttpBodyConverters.CompressDictionaryIntoResponseBodyBSG(
                new Dictionary<string, object>() { { "utc_time", (int)timeSpan.TotalSeconds } }
                , Request, Response);

        }

        [Route("client/game/version/validate")]
        [HttpPost]
        public async void VersionValidate(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressNullIntoResponseBodyBSG(Request, Response);
        }
    }
}
