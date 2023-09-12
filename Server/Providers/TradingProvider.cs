using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            foreach(var traderDirectory in Directory.GetDirectories(TradersAssetPath).Select(x => new DirectoryInfo(x)))
            {
                if(traderDirectory.Name.Contains("ragfair"))
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

            if(!DatabaseProvider.TryLoadTemplateFile("handbook.json", out var handbookTemplates))
                return StaticPrices;

            var handbookTemplateItems = handbookTemplates["Items"] as JArray;

            Dictionary<string, dynamic> templateDictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(templates); 
            foreach(var template in templateDictionary)
            {
                if(template.Value._type == "Item")
                {
                    if(!StaticPrices.ContainsKey(template.Key))
                    {
                        if (handbookTemplateItems.Any(x => x["Id"].ToString() == template.Key))
                        {
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
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "assort.json"), out JObject assort);
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "base.json"), out JObject b);
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "dialogue.json"), out JObject dialogue);
            DatabaseProvider.TryLoadDatabaseFile(Path.Combine("traders", traderId, "questassort.json"), out JObject questAssort);

            return new Trader(assort, b, dialogue, questAssort);
        }

        public enum EMoney
        {
            ROUBLES,
            EUROS,
            DOLLARS
        }

        public class Trader
        {
            public Trader(in JObject assort, in JObject ba, in JObject dialogue, in JObject questAssort) 
            { 
                Assort = assort;
                Base = ba;
                Dialogue = dialogue;
                QuestAssort = questAssort;
            }

            public JObject Assort { get; set; }
            public JObject Base { get; set; }
            public JObject Dialogue { get; set; }
            public JObject QuestAssort { get; set; }
        }
    }
}
