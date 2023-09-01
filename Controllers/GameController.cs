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
        public async void Start()
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);
            await HttpBodyConverters.CompressDictionaryIntoResponseBodyBSG(
                new Dictionary<string, object>() { { "utc_time", DateTime.UtcNow.Ticks / 1000 } }
                , Response);
        }
    }
}
