using Newtonsoft.Json;
using System.Diagnostics;

namespace SIT.WebServer.Providers
{
    public class DatabaseProvider
    {
        public static string DatabaseAssetPath { get { return Path.Combine(AppContext.BaseDirectory, "assets", "database"); } }


        public static bool TryLoadLocales(out Dictionary<string, string> locales, out Dictionary<string, Dictionary<string, object>> localesDict, out Dictionary<string, object> languages)
        {
            bool result = false;

            var localesPath = Path.Combine(DatabaseAssetPath, "locales");

            locales = new();
            localesDict = new();
            languages = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(Path.Combine(localesPath, "languages.json")));
            string stuff = localesPath;
            var dirs = Directory.GetDirectories(localesPath);
            foreach (var dir in dirs)
            {
                if (dir.EndsWith("menu"))
                {
                    
                }
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
                {
                    string localename = dir.Replace(stuff + "\\", "");
                    string localename_add = file.Replace(dir + "\\", "").Replace(".json", "");
                    locales.Add(localename + "_" + localename_add, File.ReadAllText(file));
                    localesDict.Add(localename + "_" + localename_add, JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(file)));

                    result = true;
                }
            }

            return result;
        }
    }
   
}
