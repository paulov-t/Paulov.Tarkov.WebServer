using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.WebServer.BSG;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;
using System.Diagnostics;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static BackendSession0;
using static SIT.WebServer.Providers.TradingProvider;
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
                return HttpSessionHelpers.GetSessionId(Request, HttpContext);
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
            string externalIP = Program.publicIp;
            string port = "6969";

            string resolvedIp = $"{protocol}{externalIP}:{port}";
#pragma warning restore SYSLIB0014 // Type or member is obsolete

            var sessionId = SessionId;
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
                , { "aid", profile.AccountId }
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
            var sessionId = SessionId;

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
                    //list.Add(saveProvider.GetPmcProfile(sessionId));
                    list.Add(saveProvider.GetScavProfile(sessionId));
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
            pmcData["aid"] = $"{profile.AccountId}";
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
            profile.Characters["scav"] = null;
            profile.Info["wipe"] = false;

            saveProvider.CleanIdsOfInventory(profile);
            saveProvider.SaveProfile(SessionId, profile);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(profile), Request, Response);
            requestBody = null;

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
            try
            {
                Dictionary<string, dynamic> responseNotifier = new NotifierProvider().CreateNotifierPacket(SessionId);
                response.Add("notifier", responseNotifier);
                response.Add("notifierServer", $"{responseNotifier["notifierServer"]}");
            }
            catch (Exception)
            {
                response.Add("notifier", new JObject());
                response.Add("notifierServer", new JObject());
            }
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(response), Request, Response);
            requestBody = null;
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
            profileScav.Add("profileToken", null);
            profileScav.Add("status", "Free");
            profileScav.Add("sid", $"");
            profileScav.Add("ip", $"");
            profileScav.Add("port", 0);
            profileScav.Add("version", "live");
            profileScav.Add("location", "bigmap");
            profileScav.Add("raidMode", "Online");
            profileScav.Add("mode", "deathmatch");
            profileScav.Add("shortId", "xxx1x1");
            Dictionary<string, dynamic> profilePmc = new();
            profilePmc.Add("profileid", $"pmc{SessionId}");
            profilePmc.Add("profileToken", null);
            profilePmc.Add("status", "Free");
            profilePmc.Add("sid", $"");
            profilePmc.Add("ip", $"");
            profilePmc.Add("port", 0);
            responseProfiles.Add(profileScav);
            responseProfiles.Add(profilePmc);
            response.Add("profiles", responseProfiles);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(response), Request, Response);
            requestBody = null;
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

        [Route("client/weather")]
        [HttpPost]
        public async void Weather(
          [FromQuery] int? retry
      , [FromQuery] bool? debug
         )
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBody(Request);
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new WeatherProvider.WeatherClass()), Request, Response);
        }


        [Route("client/handbook/templates")]
        [HttpPost]
        public async void HandbookTemplates(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadTemplateFile("handbook.json", out var templates);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(templates), Request, Response);

        }

        [Route("client/hideout/areas")]
        [HttpPost]
        public async void HideoutAreas(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("hideout", "areas.json"), out JArray jobj);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(jobj), Request, Response);

        }


        [Route("client/hideout/qte/list")]
        [HttpPost]
        public async void HideoutQTEList(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("hideout", "qte.json"), out JArray jobj);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(jobj), Request, Response);

        }


        [Route("client/hideout/settings")]
        [HttpPost]
        public async void HideoutSettings(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("hideout", "settings.json"), out JObject jobj);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(jobj), Request, Response);

        }

        [Route("client/hideout/production")]
        [Route("client/hideout/production/recipes")]
        [HttpPost]
        public async void HideoutProduction(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("hideout", "production.json"), out JArray jobj);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(jobj), Request, Response);

        }

        [Route("client/hideout/scavcase")]
        [Route("client/hideout/production/scavcase/recipes")]
        [HttpPost]
        public async void HideoutScavcase(int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("hideout", "scavcase.json"), out JArray jobj);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(jobj), Request, Response);

        }

        [Route("client/handbook/builds/my/list")]
        [HttpPost]
        public async void UserPresets(int? retry, bool? debug)
        {
            Dictionary<string, object> nullResult = new Dictionary<string, object>();
            nullResult.Add("equipmentBuilds", new JArray());
            nullResult.Add("weaponBuilds", new JArray());
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(nullResult), Request, Response);

        }

        [Route("client/notifier/channel/create")]
        [HttpPost]
        public async void NotifierChannelCreate(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new NotifierProvider().CreateNotifierPacket(SessionId)), Request, Response);

        }

        

        //
        //
        //


        [Route("client/mail/dialog/list")]
        [HttpPost]
        public async void MailDialogList(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new JArray()), Request, Response);
        }

        [Route("client/trading/customization/storage")]
        [HttpPost]
        public async void CustomizationStorage(int? retry, bool? debug)
        {
            Dictionary<string, object> packetResult = new Dictionary<string, object>();
            packetResult.Add("_id", $"pmc{SessionId}");
            packetResult.Add("suites", saveProvider.LoadProfile(SessionId).Suits);
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(packetResult), Request, Response);
        }

        [Route("client/friend/request/list/inbox")]
        [HttpPost]
        public async void FriendRequestInbox(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new JArray()), Request, Response);
        }

        [Route("client/friend/request/list/outbox")]
        [HttpPost]
        public async void FriendRequestOutbox(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new JArray()), Request, Response);
        }

        [Route("client/friend/list")]
        [HttpPost]
        public async void FriendList(int? retry, bool? debug)
        {
            Dictionary<string, object> packet = new Dictionary<string, object>();
            packet.Add("Friends", new JArray());
            packet.Add("Ignore", new JArray());
            packet.Add("InIgnoreList", new JArray());
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(packet), Request, Response);
        }

        [Route("client/server/list")]
        [HttpPost]
        public async void ServerList(int? retry, bool? debug)
        {
            string externalIP = Program.publicIp;
            string port = "6969";
            Dictionary<string, object> packet = new Dictionary<string, object>();
            packet.Add("ip", externalIP);
            packet.Add("port", port);

            var packets = new List<Dictionary<string, object>>();
            packets.Add(packet);
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(packets), Request, Response);
        }

        [Route("client/match/group/current")]
        [HttpPost]
        public async void GroupCurrent(int? retry, bool? debug)
        {
            Dictionary<string, object> packet = new();
            packet.Add("squad", new JArray());
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(packet), Request, Response);
        }

        [Route("client/quest/list")]
        [HttpPost]
        public async void QuestList(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new JArray()), Request, Response);
        }

        [Route("client/repeatalbeQuests/activityPeriods")]
        [HttpPost]
        public async void RepeatableQuestList(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new JArray()), Request, Response);
        }

        [Route("player/health/sync")]
        [HttpPost]
        public async void HealthSync(int? retry, bool? debug)
        {
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(new JObject()), Request, Response);
        }

        [Route("/client/items/prices/{traderId}")]
        [HttpPost]
        public async void ItemPricesForTraderId(int? retry, bool? debug)
        {
            var tradingProvider = new TradingProvider();
            Dictionary<string, int> handbookPrices = tradingProvider.GetStaticPrices();
            Dictionary<string, object> packet = new Dictionary<string, object>();
            packet.Add("supplyNextTime", 0);
            packet.Add("prices", handbookPrices);
            packet.Add("currencyCourses", 
                new Dictionary<string, object>() {
                    { "5449016a4bdc2d6f028b456f", handbookPrices["5449016a4bdc2d6f028b456f"] },
                    {  "569668774bdc2da2298b4568", handbookPrices["569668774bdc2da2298b4568"] },
                    { "5696686a4bdc2da3298b456a", handbookPrices["5696686a4bdc2da3298b456a"] }
                }
                );
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(packet), Request, Response);
        }

        [Route("/client/trading/api/getTraderAssort/{traderId}")]
        [HttpPost]
        public async void GetTraderAssort(int? retry, bool? debug, string traderId)
        {
            var saveProvider = new SaveProvider();
            var profile = saveProvider.LoadProfile(SessionId);
            var tradingProvider = new TradingProvider();

            //Dictionary<string, object> packet = new Dictionary<string, object>();
            EFT.TraderAssortment traderAssortment = new EFT.TraderAssortment();
            traderAssortment.Items = new List<Items>().ToArray();
            traderAssortment.BarterScheme = new Dictionary<string, EFT.BarterScheme>();
            traderAssortment.LoyaltyLevelItems = new Dictionary<string, int>();

            //packet.Add("nextResupply", 1000000);
            //packet.Add("items", new JArray());
            //packet.Add("barter_scheme", new Dictionary<string, object>() { });
            //packet.Add("loyal_level_items", new Dictionary<string, object>() { });

            if (traderId == "ragfair")
            {
                await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(traderAssortment), Request, Response);
                return;
            }

            var trader = tradingProvider.GetTraderById(traderId);
            var traderAssortmentForPlayer = tradingProvider.GetTraderAssortmentById(traderId, SessionId);
            //var loyalLevelItems = trader.Assort["loyal_level_items"];


            //packet["items"] = trader.Assort["items"];
            //packet["barter_scheme"] = trader.Assort["barter_scheme"];
            //packet["loyal_level_items"] = trader.Assort["loyal_level_items"];
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(traderAssortment), Request, Response);
        }

        [Route("/client/game/profile/items/moving")]
        [HttpPost]
        public async void ItemsMoving(int? retry, bool? debug)
        {
            QueueData queueData = new QueueData();
            queueData.ProfileChanges = new Dictionary<string, Changes>();
            queueData.InventoryWarnings = new InventoryWarning[0];

            try
            {
                var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);
                var sessionId = SessionId;
                var saveProvider = new SaveProvider();
                var pmcProfile = saveProvider.GetPmcProfile(sessionId);

                JArray data = (JArray)requestBody["data"];
                foreach (var actionData in data)
                {
                    var action = actionData["Action"].ToString();

                    string type = null;
                    if (actionData["type"] != null)
                        type = actionData["type"].ToString();

                    JToken item;
                    if (actionData["item"] != null)
                        item = actionData["item"];

                    JToken to;
                    if (actionData["to"] != null)
                        to = actionData["to"];




                    IEnumerable<JToken> items = null;
                    if (actionData["items"] != null)
                        items = actionData["items"].ToArray();

                    switch (action)
                    {
                        case "Move":


                            break;
                        // Buying Selling from Trader
                        case "TradingConfirm":
                            if (items == null)
                                break;

                            switch (type)
                            {
                                case "sell_to_trader":

                                    var processSellTradeData = actionData.ToObject<ProcessSellTradeRequestData>();

                                    //queueData.ProfileChanges.Add(new MongoID(true), new Changes() { Stash = new StashChanges() { del = new List<Items>() } });

                                    for (var iIt = 0; iIt < processSellTradeData.items.Count(); iIt++)
                                    //foreach (var it in processSellTradeData.items)
                                    {
                                        var it = processSellTradeData.items[iIt];   
                                        var itemIdToFind = it.id.Trim();
                                        var inv = (JToken)pmcProfile["Inventory"];
                                        var invItems = (JArray)inv["items"];
                                        //foreach (var invItem in invItems)
                                        var deletedItemsCount = 0;
                                        for(var iInvItem = 0; iInvItem < invItems.Count; iInvItem++)
                                        {
                                            var invItem = invItems[iInvItem];
                                            var _id = invItem["_id"].ToString().Trim();
                                            var _tpl = invItem["_tpl"].ToString().Trim();
                                            if (_id == itemIdToFind || _id == itemIdToFind)
                                            {
                                                Debug.WriteLine($"selling {_id} {_tpl}");
                                                if (!queueData.ProfileChanges.ContainsKey(sessionId))
                                                    queueData.ProfileChanges.Add(sessionId, new Changes());

                                                if (queueData.ProfileChanges[sessionId].Stash == null)
                                                {
                                                    queueData.ProfileChanges[sessionId].Stash = new StashChanges()
                                                    {
                                                        del = new Items[processSellTradeData.items.Length]
                                                    };
                                                }

                                                queueData.ProfileChanges[sessionId].Stash.del[iIt] = (new Items() { _id = _id, _tpl = _tpl, location = invItem["location"].ToObject<UnparsedData>(), parentId = invItem["parentId"].ToString(), slotId = invItem["slotId"].ToString() });
                                                deletedItemsCount++;
                                                if (deletedItemsCount == processSellTradeData.items.Length)
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }

                            break;
                        // Buying an Offer from Flea
                        case "RagFairBuyOffer":
                            break;
                        // The Sell All button after a Scav Raid
                        case "SellAllFromSavage":
                            break;
                    }

                }


                foreach(var kvpProfileChanges in queueData.ProfileChanges)
                {
                    saveProvider.ProcessProfileChanges(kvpProfileChanges.Key, kvpProfileChanges.Value);
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(queueData), Request, Response);
        }

        [Route("/client/checkVersion")]
        [HttpPost]
        public async void CheckVersion(int? retry, bool? debug)
        {
            Dictionary<string, object> packet = new Dictionary<string, object>();
            packet.Add("isValid", true);
            packet.Add("latestVersion", "");
            await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(packet), Request, Response);
        }
    }
}
