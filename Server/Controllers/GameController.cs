using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIT.WebServer.Controllers
{
    [ApiController]
    public class GameController : ControllerBase
    {
        private TradingProvider tradingProvider { get; } = new TradingProvider();
        private SaveProvider saveProvider { get; } = new SaveProvider();

        private string SessionId
        {
            get
            {
                var sessId = HttpContext.Session.GetString("SessionId");
                return sessId;
            }
        }

        private int AccountId
        {
            get
            {
                var aid = HttpContext.Session.GetInt32("AccountId");
                return aid.Value;
            }
        }

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
                Response.StatusCode = 412; // Precondition
                return;
            }

            var profile = saveProvider.LoadProfile(sessionId);
            var pmcProfile = saveProvider.GetPmcProfile(sessionId);
            int aid = int.Parse(profile.Info["aid"].ToString());
            HttpContext.Session.SetInt32("AccountId", aid);

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
            if (DatabaseProvider.TryLoadItemTemplates(out var items, count, page))
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
            if (DatabaseProvider.TryLoadGlobals(out var items))
                await HttpBodyConverters.CompressIntoResponseBodyBSG(items, Request, Response);

        }

        [Route("client/trading/api/traderSettings")]
        [HttpPost]
        public async void TraderSettings(int? retry, bool? debug)
        {
            if (TradingProvider.TryLoadTraders(out var items))
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
            if (profile == null)
            {
                Response.StatusCode = 500;
                return;
            }

            var profileInfo = profile.Info as dynamic;
            if (profileInfo != null)
            {
                List<object> list = new List<object>();
                if ((bool)profileInfo["wipe"])
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

            if (requestBody["nickname"].ToString().Length < 3)
            {
                await HttpBodyConverters.CompressIntoResponseBodyBSG(null, Request, Response, 256, "The nickname is too short");
                return;
            }
            else if (saveProvider.NameExists(requestBody["nickname"].ToString()))
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

        [Route("client/game/profile/create")]
        [HttpPost]
        public async void ProfileCreate(
            [FromQuery] int? retry
            , bool? debug
           )
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            //var requestBody2 = await HttpBodyConverters.DecompressRequestBody(Request);



            var profile = saveProvider.LoadProfile(SessionId);
            if (profile == null)
            {
                Response.StatusCode = 500;
                return;
            }

            if (!DatabaseProvider.TryLoadDatabaseFile("templates/profiles.json", out JObject profiles))
            {
                Response.StatusCode = 500;
                return;
            }

            // Get Template Profile
            var templateProfile = profiles[(string)profile.Info["edition"]][requestBody["side"].ToString().ToLower()].ToObject<Dictionary<string, dynamic>>();
            if (templateProfile == null)
            {
                Response.StatusCode = 500;
                return;
            }



            if (!DatabaseProvider.TryLoadCustomization(out var customization))
            {
                Response.StatusCode = 500;
                return;
            }

            var pmcData = ((JToken)templateProfile["character"]).ToObject<Dictionary<string, dynamic>>();
            pmcData["_id"] = $"pmc{SessionId}";
            pmcData["aid"] = $"{AccountId}";
            pmcData["savage"] = $"scav{SessionId}";
            pmcData["sessionId"] = $"{SessionId}";
            if (requestBody == null)
            {
                Response.StatusCode = 412; // pre condition
                return;
            }

            if (!requestBody.ContainsKey("nickname"))
            {
                Response.StatusCode = 412; // pre condition
                return;
            }

            var pmcDataInfo = ((JToken)pmcData["Info"]).ToObject<Dictionary<string, dynamic>>();
            pmcDataInfo["Nickname"] = requestBody["nickname"].ToString();
            pmcDataInfo["LowerNickname"] = requestBody["nickname"].ToString().ToLower();
            pmcDataInfo["RegistrationDate"] = (int)Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            pmcDataInfo["Voice"] = ((JToken)customization[requestBody["voiceId"].ToString()])["_name"];
            pmcData["Info"] = pmcDataInfo;

            var pmcCustomizationInfo = ((JToken)pmcData["Customization"]).ToObject<Dictionary<string, dynamic>>();
            pmcCustomizationInfo["Head"] = requestBody["headId"].ToString();
            pmcData["Customization"] = pmcCustomizationInfo;

            profile.Characters["pmc"] = pmcData;
            profile.Info["wipe"] = false;
            saveProvider.SaveProfile(SessionId, profile);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(profile), Request, Response);

        }

        [Route("client/game/profile/select")]
        [HttpPost]
        public async void ProfileSelect(
            [FromQuery] int? retry
        , [FromQuery] bool? debug
           )
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            Dictionary<string, dynamic> response = new();
            response.Add("status", "ok");
            Dictionary<string, dynamic> responseNotifier = new();
            responseNotifier.Add("server", "");
            responseNotifier.Add("channel_id", $"{SessionId}");
            responseNotifier.Add("url", "");
            responseNotifier.Add("notifierServer", "");
            responseNotifier.Add("ws", "");
            response.Add("notifier", responseNotifier);
            response.Add("notifierServer", $"http://127.0.0.1/notifierServer/get/{SessionId}");


            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(response), Request, Response);
        }

        [Route("client/profile/status")]
        [HttpPost]
        public async void ProfileStatus(
            [FromQuery] int? retry
        , [FromQuery] bool? debug
           )
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            Dictionary<string, dynamic> response = new();
            response.Add("maxPveCountExceeded", false);
            List<Dictionary<string, dynamic>> responseProfiles = new();
            Dictionary<string, dynamic> profileScav = new();
            profileScav.Add("profileid", $"scav{SessionId}");
            Dictionary<string, dynamic> profilePmc = new();
            profilePmc.Add("profileid", $"pmc{SessionId}");
            responseProfiles.Add(profileScav);
            responseProfiles.Add(profilePmc);
            response.Add("profiles", responseProfiles);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(response), Request, Response);
        }

        [Route("client/locations")]
        [HttpPost]
        public async void Locations(
          [FromQuery] int? retry
      , [FromQuery] bool? debug
         )
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            if (!DatabaseProvider.TryLoadLocations(out Dictionary<string, Dictionary<string, object>> locations))
            {
                Response.StatusCode = 500;
                return;
            }

            Dictionary<string, Dictionary<string, object>> response = new();

            foreach (var kvp in locations) 
            {
            
            }


            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(response), Request, Response);
        }
    }
}
