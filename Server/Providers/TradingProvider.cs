using Newtonsoft.Json.Linq;

namespace SIT.WebServer.Providers
{
    public class TradingProvider
    {
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
    }
}
