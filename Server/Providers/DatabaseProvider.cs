using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace SIT.WebServer.Providers
{
    public class DatabaseProvider
    {
        public static string DatabaseAssetPath { get { return Path.Combine(AppContext.BaseDirectory, "assets", "database"); } }

        public static Dictionary<string, object> Database { get; } = new Dictionary<string, object>();

        static DatabaseProvider()
        {
            //TryLoadLocales(out _, out _, out _);
            TryLoadLocations(out _);
        }


        public static bool TryLoadLocales(
            out Dictionary<string, string> locales
            , out Dictionary<string, Dictionary<string, object>> localesDict
            , out Dictionary<string, object> languages)
        {
            bool result = false;

            var localesPath = Path.Combine(DatabaseAssetPath, "locales");

            locales = new();
            localesDict = new();
            languages = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(Path.Combine(localesPath, "languages.json")));
            string basePath = localesPath;
            var dirs = Directory.GetDirectories(localesPath);
            foreach (var dir in dirs)
            {
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
                {
                    string localename = dir.Replace(basePath + "\\", "");
                    string localename_add = file.Replace(dir + "\\", "").Replace(".json", "");
                    locales.Add(localename + "_" + localename_add, File.ReadAllText(file));
                    localesDict.Add(localename + "_" + localename_add, JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(file)));

                    result = true;
                }
            }

            return result;
        }

        public static bool TryLoadDatabaseFile(
        in string databaseFilePath,
        out Dictionary<string, object> templates)
        {
            bool result = false;

            var filePath = Path.Combine(DatabaseAssetPath, databaseFilePath);

            var stringTemplates = File.ReadAllText(filePath);
            result = stringTemplates != null;
            templates = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringTemplates);

            return result;
        }

        public static bool TryLoadDatabaseFile(
        in string databaseFilePath,
        out JObject dbFile)
        {
            bool result = false;

            var filePath = Path.Combine(DatabaseAssetPath, databaseFilePath);

            dbFile = JObject.Parse(File.ReadAllText(filePath));
            result = dbFile != null;
            return result;
        }

        public static bool TryLoadDatabaseFile(
        in string databaseFilePath,
        out string stringTemplates)
        {
            bool result = false;

            var filePath = Path.Combine(DatabaseAssetPath, databaseFilePath);

            stringTemplates = File.ReadAllText(filePath);
            result = stringTemplates != null;
            return result;
        }


        public static bool TryLoadTemplateFile(
         in string templateFile,
         out Dictionary<string, object> templates)
        {
            bool result = false;

            var filePath = Path.Combine(DatabaseAssetPath, "templates", templateFile);

            var stringTemplates = File.ReadAllText(filePath);
            result = stringTemplates != null;
            templates = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringTemplates);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="count">Used for Swagger tests - count on page</param>
        /// <param name="page">Used for Swagger tests - page</param>
        /// <returns></returns>
        public static bool TryLoadItemTemplates(

            //out Dictionary<string, object> templates,
            out string templatesRaw,
            in int? count = null,
            in int? page = null
            )
        {
            var itemsPath = Path.Combine(DatabaseAssetPath, "templates", "items.json");

            var stringTemplates = File.ReadAllText(itemsPath);

            templatesRaw = stringTemplates;
            return templatesRaw != null;
        }

        public static bool TryLoadCustomization(
          out Dictionary<string, object> customization)
        {
            return TryLoadTemplateFile("customization.json", out customization);
        }

        public static bool TryLoadGlobals(
         out Dictionary<string, object> globals)
        {
            return TryLoadDatabaseFile("globals.json", out globals);
        }

        public static bool TryLoadLocations(
         out Dictionary<string, Dictionary<string, object>> locations)
        {
            Dictionary<string, Dictionary<string,object>> locationsRaw = new();
            foreach (var dir in Directory.GetDirectories(Path.Combine(DatabaseAssetPath, "locations")).Select(x => new DirectoryInfo(x)))
            {
                locationsRaw.Add(dir.Name, new Dictionary<string, object>());
                foreach (var f in Directory.GetFiles(dir.FullName).Select(x => new FileInfo(x)))
                {
                    locationsRaw[dir.Name].Add(f.Name, JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(f.FullName)));
                }
            }

            if(!Database.ContainsKey("locations"))
                Database.Add("locations", locationsRaw);

            locations = locationsRaw;
            return locations.Count > 0;
        }
    }

}
