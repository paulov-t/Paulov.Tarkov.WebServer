using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SIT.WebServer.Providers
{
    public class DatabaseProvider
    {
        public static string DatabaseAssetPath { get { return Path.Combine(AppContext.BaseDirectory, "assets", "database"); } }

        public static Dictionary<string, object> Database { get; } = new Dictionary<string, object>();

        static DatabaseProvider()
        {
            //TryLoadLocales(out _, out _, out _);
            //TryLoadLocations(out _);
        }

        private static T StreamFileToType<T>(string path)
        {
            using (var readerLanguagesJson = new StreamReader(path))
            {
                using (var readerLanguagesJsonTR = new JsonTextReader(readerLanguagesJson))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    return serializer.Deserialize<T>(readerLanguagesJsonTR);
                }
            }
            //return System.Text.Json.JsonDocument.Parse(File.ReadAllText(path)).Deserialize<T>();
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
            languages = StreamFileToType<Dictionary<string, object>>(Path.Combine(localesPath, "languages.json"));
            //languages = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(localesPath, "languages.json"))).Deserialize<Dictionary<string, object>>();

            string basePath = localesPath;
            var dirs = Directory.GetDirectories(localesPath);
            foreach (var dir in dirs)
            {
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
                {
                    string localename = dir.Replace(basePath + "\\", "");
                    string localename_add = file.Replace(dir + "\\", "").Replace(".json", "");

                    using (var sr = new StreamReader(file))
                        locales.Add(localename + "_" + localename_add, sr.ReadToEnd());

                    //localesDict.Add(localename + "_" + localename_add, JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(file)));
                    localesDict.Add(localename + "_" + localename_add, StreamFileToType<Dictionary<string, object>>(file));


                    result = true;
                }
                files = null;
            }
            dirs = null;

            return result;
        }

        public static bool TryLoadLanguages(
            out JObject languages)
        {
            var localesPath = Path.Combine(DatabaseAssetPath, "locales");
            languages = JObject.Parse(File.ReadAllText((Path.Combine(localesPath, "languages.json"))));
            return true;
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
            //var jsonDocument = System.Text.Json.JsonDocument.Parse(File.ReadAllText(filePath));

            //dbFile = jsonDocument.Deserialize<JObject>(); // JObject.Parse(File.ReadAllText(filePath));
            if (!File.Exists(filePath))
            {
                dbFile = null;
                return false;
            }

            dbFile = JObject.Parse(File.ReadAllText(filePath));
            result = dbFile != null;
            return result;
        }

        public static bool TryLoadDatabaseFile(
        in string databaseFilePath,
        out JsonDocument jsonDocument)
        {
            bool result = false;

            var filePath = Path.Combine(DatabaseAssetPath, databaseFilePath);
            jsonDocument = System.Text.Json.JsonDocument.Parse(filePath);
            result = jsonDocument != null;
            return result;
        }

        public static bool TryLoadDatabaseFile(
        in string databaseFilePath,
        out JArray dbFile)
        {
            bool result = false;

            var filePath = Path.Combine(DatabaseAssetPath, databaseFilePath);

            dbFile = JArray.Parse(File.ReadAllText(filePath));
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
            //return TryLoadDatabaseFile("globals.json", out globals);
            return TryLoadDatabaseFile("globalsArena.json", out globals);
        }

        public static bool TryLoadGlobalsArena(
         out Dictionary<string, object> globals)
        {
            var result = TryLoadDatabaseFile("globals.json", out globals);
            result = TryLoadDatabaseFile("globalsArena.json", out Dictionary<string, object> globalsArena);
            globals.Add("GlobalsArena", globalsArena);
            globals.Add("GameModes",
                new Dictionary<string, object>()
                );
            return result;
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

        public static bool TryLoadWeather(
         out Dictionary<string, Dictionary<string, object>> weather)
        {
            weather = new();
            return true;
        }
    }

}
