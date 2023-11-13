using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIT.WebServer.Providers
{
    public class TradingProvider
    {
        public static Dictionary<EMoney, string> MoneyToString = new() { { EMoney.ROUBLES, "5449016a4bdc2d6f028b456f" }, { EMoney.EUROS, "569668774bdc2da2298b4568" }, { EMoney.DOLLARS, "5696686a4bdc2da3298b456a" } };

        public static Dictionary<string, int> StaticPrices = new();

        public static string DatabaseAssetPath => DatabaseProvider.DatabaseAssetPath;
        public static string TradersAssetPath => Path.Combine(DatabaseProvider.DatabaseAssetPath, "traders");

        static TradingProvider()
        {
            TryLoadTraders(out _);
        }

        public static bool TryLoadTraders(
         out Dictionary<string, object> traderByTraderId)
        {
            traderByTraderId = new Dictionary<string, object>();
            foreach (var traderDirectory in Directory.GetDirectories(TradersAssetPath).Select(x => new DirectoryInfo(x)))
            {
                if (traderDirectory.Name.Contains("ragfair"))
                    continue;

                traderByTraderId.Add(traderDirectory.Name, JObject.Parse(File.ReadAllText(Path.Combine(traderDirectory.FullName, "base.json"))));
            }
            return traderByTraderId.Count > 0;
        }

        internal Dictionary<string, int> GetStaticPrices()
        {
            if (StaticPrices.Count > 0)
                return StaticPrices;

            if (!DatabaseProvider.TryLoadItemTemplates(out var templates))
                return StaticPrices;

            if (!DatabaseProvider.TryLoadTemplateFile("handbook.json", out var handbookTemplates))
                return StaticPrices;

            var handbookTemplateItems = handbookTemplates["Items"] as JArray;

            Dictionary<string, JObject> templateDictionary = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(templates);
            foreach (var template in templateDictionary)
            {
                if (template.Value == null)
                    continue;

                if (!((JObject)template.Value).TryGetValue("_type", out var typeObj))
                    continue;

                if (typeObj.ToString() == "Item")
                {
                    if (!StaticPrices.ContainsKey(template.Key))
                    {
                        if (handbookTemplateItems.Any(x => x["Id"].ToString() == template.Key))
                        {
                            if (!StaticPrices.ContainsKey(template.Key))
                                StaticPrices.Add(template.Key, int.Parse(handbookTemplateItems.Single(x => x["Id"].ToString() == template.Key)["Price"].ToString()));
                        }
                        else
                        {
                            StaticPrices.Add(template.Key, 1);
                        }
                    }
                }
            }

            return StaticPrices;
        }

        internal Trader GetTraderById(string traderId)
        {
            var assortJsonPath = Path.Combine("traders", traderId, "assort.json");
            DatabaseProvider.TryLoadDatabaseFile(assortJsonPath, out JObject assort);
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "base.json"), out JObject b);
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "dialogue.json"), out JObject dialogue);
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "questassort.json"), out JObject questAssort);

            var traderAssortment = assort.ToObject<EFT.TraderAssortment>();
            var trader = new Trader(traderAssortment, b, dialogue, questAssort);

            return trader;
        }

        internal EFT.TraderAssortment GetTraderAssortmentById(string traderId, string profileId)
        {
            var baseTraderAssort = GetTraderById(traderId).Assort;

            var saveProvider = new SaveProvider();
            var profile = saveProvider.LoadProfile(profileId);
            //var pmcProfile = saveProvider.GetPmcProfile(profileId);

            var resultTraderAssort = new EFT.TraderAssortment();
            foreach (var lli in baseTraderAssort.LoyaltyLevelItems)
            {

            }
            baseTraderAssort.NextResupply = 1631489718;
            baseTraderAssort.ExchangeRate = 1;
            return baseTraderAssort;
        }

        public enum EMoney
        {
            ROUBLES,
            EUROS,
            DOLLARS
        }

        public class Trader
        {
            public Trader(in EFT.TraderAssortment assort, in JObject ba, in JObject dialogue, in JObject questAssort)
            {
                Assort = assort;
                Base = ba;
                Dialogue = dialogue;
                QuestAssort = questAssort;
            }

            public EFT.TraderAssortment Assort { get; set; }
            public JObject Base { get; set; }
            public JObject Dialogue { get; set; }
            public JObject QuestAssort { get; set; }
        }

        public class ProcessSellTradeRequestData
        {
            public string Action { get; set; } = "sell_to_trader";
            public string type { get; set; }
            public string tid { get; set; }
            public string price { get; set; }
            public TradeItem[] items { get; set; }

        }

        public class ProcessTo
        {
            public string id { get; set; }
            public string container { get; set; }
            public ProcessToLocation location { get; set; }
            public bool isSearched { get; set; }

        }

        public class ProcessToLocation
        {
            public int x { get; set; }
            public int y { get; set; }
            public string r { get; set; }
            public string rotation { get; set; }
            public bool isSearched { get; set; }

        }

        public class TradeItem
        {
            public string id { get; set; }
            public int count { get; set; }
            public string scheme_id { get; set; }
        }
    }
}
