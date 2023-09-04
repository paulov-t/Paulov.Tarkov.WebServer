using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;
using System.Text;

namespace SIT.WebServer.Controllers
{
    [ApiController]
    public class GameController : ControllerBase
    {
        private TradingProvider tradingProvider { get; } = new TradingProvider();
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

        [Route("client/game/config")]
        [HttpPost]
        public async void GameConfig(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadLocales(out var locales, out var localesDict, out var languages);

#pragma warning disable SYSLIB0014 // Type or member is obsolete
            //string externalIP = new System.Net.WebClient().DownloadString("https://api.ipify.org");
            string protocol = "http://";
            string externalIP = "127.0.0.1";// new System.Net.WebClient().DownloadString("https://api.ipify.org");
            string port = "6969";

            string resolvedIp = $"{protocol}{externalIP}:{port}";
#pragma warning restore SYSLIB0014 // Type or member is obsolete

            var sessionId = HttpSession.GetSessionId(Request.Headers);
            if (string.IsNullOrEmpty(sessionId))
            {
                if (HttpContext.Session.TryGetValue("SessionId", out var sessionIdBytes))
                {
                    sessionId = Encoding.UTF8.GetString(sessionIdBytes);
                }
            }
            else
            {
                HttpContext.Session.SetString("SessionId", sessionId);
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                Response.StatusCode = 405;
                return;
            }

            var profile = saveProvider.GetPmcProfile(sessionId);

            var config = new Dictionary<string, object>()
            {
                { "languages", languages }
                , { "ndaFree", true }
                , { "reportAvailable", false }
                , { "twitchEventMember", false }
                , { "lang", "en" }
                , { "aid", sessionId }
                , { "taxonomy", 6 }
                , { "activeProfileId", $"pmc{sessionId}" }
                , { "backend",
                    new { Lobby = resolvedIp, Trading = resolvedIp, Messaging = resolvedIp, Main = resolvedIp, Ragfair = resolvedIp }
                }
                , { "useProtobuf", false }
                , { "utc_time", DateTime.UtcNow.Ticks / 1000 }
                , { "totalInGame", 1 }
            };

            await HttpBodyConverters.CompressIntoResponseBodyBSG(config, Request, Response);
        }

        [Route("client/items")]
        [HttpPost]
        [HttpGet]
        public async void TemplateItems(int? retry, bool? debug, int? count, int? page)
        {
            if(DatabaseProvider.TryLoadItemTemplates(out var items, count, page))
                await HttpBodyConverters.CompressIntoResponseBodyBSG(items, Request, Response);
            else
                Response.StatusCode = 500;

        }

        [Route("client/customization")]
        [HttpPost]
        public async void Customization(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadCustomization(out var items);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(items, Request, Response);

        }

        [Route("client/globals")]
        [HttpPost]
        public async void Globals(int? retry, bool? debug)
        {
            if(DatabaseProvider.TryLoadGlobals(out var items))
                await HttpBodyConverters.CompressIntoResponseBodyBSG(items, Request, Response);

        }

        [Route("client/trading/api/traderSettings")]
        [HttpPost]
        public async void TraderSettings(int? retry, bool? debug)
        {
            if(TradingProvider.TryLoadTraders(out var items))
                await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(items.Values), Request, Response);

        }

        [Route("client/settings")]
        [HttpPost]
        public async void Settings(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile("settings.json", out Dictionary<string, object> items);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(items, Request, Response);

        }

        [Route("client/game/profile/list")]
        [HttpPost]
        public async void ProfileList(int? retry, bool? debug)
        {
            var sessionId = HttpContext.Session.GetString("SessionId");

            var profile = saveProvider.LoadProfile(sessionId);
            var profileInfo = profile["info"] as JToken;
            if (profileInfo != null)
            {
                List<object> list = new List<object>();
                if (profileInfo["wipe"].ToObject<bool>())
                {
                }
                else
                {
                    list.Add(saveProvider.GetPmcProfile(sessionId));
                    list.Add(saveProvider.GetPmcProfile(sessionId));
                }
                await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(list), Request, Response);
            }
            

        }

        [Route("client/game/profile/nickname/reserved")]
        [HttpPost]
        public async void NicknameReserved(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG("\"StayInTarkov\"", Request, Response);

        }

        [Route("client/game/profile/nickname/validate")]
        [HttpPost]
        public async void NicknameValidate(int? retry, bool? debug)
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            if(requestBody["nickname"].ToString().Length < 3)
            {
                await HttpBodyConverters.CompressIntoResponseBodyBSG(null, Request, Response, 256, "The nickname is too short");
                return;
            }
            else if(saveProvider.NameExists(requestBody["nickname"].ToString()))
            {
                await HttpBodyConverters.CompressIntoResponseBodyBSG(null, Request, Response, 255, "The nickname is already in use");
                return;
            }

            JObject obj = new JObject();
            obj.TryAdd("status", "ok");
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(obj), Request, Response);

        }

        [Route("client/game/keepalive")]
        [HttpPost]
        public async void KeepAlive(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(true.ToString(), Request, Response);

        }

        [Route("client/account/customization")]
        [HttpPost]
        public async void AccountCustomization(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile("templates/character.json", out string items);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(items, Request, Response);

        }
    }
}
