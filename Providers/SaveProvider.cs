using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.WebServer.BSG;

namespace SIT.WebServer.Providers
{
    public class SaveProvider
    {
        public static Random Randomizer { get; } = new Random();

        static SaveProvider()
        {
            var userProfileDirectory = Path.Combine(AppContext.BaseDirectory, "user", "profiles");
            Directory.CreateDirectory(userProfileDirectory);

            
        }

        public SaveProvider()
        {
            var userProfileDirectory = Path.Combine(AppContext.BaseDirectory, "user", "profiles");
            var profileFiles = Directory.GetFiles(userProfileDirectory);
            foreach (var profileFilePath in profileFiles)
            {
                var fileInfo = new FileInfo(profileFilePath);
                var fileText = File.ReadAllText(profileFilePath);
                Profiles.Add(fileInfo.Name, JsonConvert.DeserializeObject<Dictionary<string, object>>(fileText));
            }
        }

        private Dictionary<string, object> Profiles { get; } = new Dictionary<string, object>();


        public string CreateAccount(Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return null;

            var sessionId = new MongoID(true).ToString();
            var newProfileDetails = new Dictionary<string, object>()
            {
                { "id", sessionId },
                { "aid", Randomizer.Next(1000000000, int.MaxValue) },
                { "username", parameters["username"] },
                { "password", parameters["password"] },
                { "wipe", true },
                { "edition", parameters["edition"] }
            };

            CreateProfile(newProfileDetails);
            LoadProfile(sessionId);
            SaveProfile(sessionId);

            return sessionId;
        }

        public void SaveProfile(string sessionId)
        {
            var userProfileDirectory = Path.Combine(AppContext.BaseDirectory, "user", "profiles");
            Directory.CreateDirectory(userProfileDirectory);
            var filePath = Path.Combine(userProfileDirectory, $"{sessionId}.json");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(Profiles[sessionId]));
        }

        public void LoadProfile(string sessionId)
        {

        }

        private void CreateProfile(Dictionary<string, object> newProfileDetails)
        {
            var newProfile = new Dictionary<string, object>()
            {
                { "info", newProfileDetails },
                { "characters", new Dictionary<string, object>() { { "pmc", new Dictionary<string, object>() }, { "scav", new Dictionary<string, object>() } } }
            };
            Profiles.Add(newProfileDetails["id"].ToString(), newProfile);
        }

        public bool ProfileExists(string username, out string sessionId)
        {
            sessionId = null;
            foreach (var profile in Profiles.Values.Select(x => (Dictionary<string, object>)x))
            {
                var info = (JObject)profile["info"];
                var infoUsername = info["username"].ToString();
                if (info["username"].ToString() == username)
                {
                    sessionId = info["id"].ToString();
                    return true;
                }
            }

            return false;

        }

        public class SaveModel
        {
            [JsonProperty("info")]
            public Dictionary<string, object> Info = new Dictionary<string, object>();

            [JsonProperty("characters")]
            public Dictionary<string, object> Characters = new Dictionary<string, object>();
        }
    }
}
