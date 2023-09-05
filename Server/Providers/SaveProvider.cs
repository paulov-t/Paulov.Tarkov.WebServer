using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.WebServer.BSG;
using System.Dynamic;

namespace SIT.WebServer.Providers
{
    public class SaveProvider
    {
        public static Random Randomizer { get; } = new Random();

        //static SaveProvider()
        //{
        //    var userProfileDirectory = Path.Combine(AppContext.BaseDirectory, "user", "profiles");
        //    Directory.CreateDirectory(userProfileDirectory);

            
        //}

        public SaveProvider()
        {
            var userProfileDirectory = Path.Combine(AppContext.BaseDirectory, "user", "profiles");
            var profileFiles = Directory.GetFiles(userProfileDirectory);
            foreach (var profileFilePath in profileFiles)
            {
                var fileInfo = new FileInfo(profileFilePath);
                if (fileInfo == null)
                    continue;

                var fileText = File.ReadAllText(profileFilePath);
                if (fileText == null)
                    continue;

                var model = JsonConvert.DeserializeObject<ProfileModel>(fileText);
                Profiles.Add(fileInfo.Name.Replace(".json",""), model);
            }
        }

        private Dictionary<string, ProfileModel> Profiles { get; } = new Dictionary<string, ProfileModel>();


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

        public void SaveProfile(string sessionId, ProfileModel profileModel = null)
        {
            if(profileModel != null)
                Profiles[sessionId] = profileModel;

            var userProfileDirectory = Path.Combine(AppContext.BaseDirectory, "user", "profiles");
            Directory.CreateDirectory(userProfileDirectory);
            var filePath = Path.Combine(userProfileDirectory, $"{sessionId}.json");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(Profiles[sessionId]));
        }

        public ProfileModel LoadProfile(string sessionId)
        {
            if(sessionId == null) return null;

            var prof = Profiles[sessionId] as ProfileModel;
            return prof;
        }

        public Dictionary<string, object> GetPmcProfile(string sessionId)
        {
            var prof = Profiles[sessionId] as ProfileModel;
            var characters = prof.Characters;
            var pmcObject = characters["pmc"];
            return pmcObject;
        }

        private void CreateProfile(Dictionary<string, object> newProfileDetails)
        {
            var newProfile = new ProfileModel();
            newProfile.Info = newProfileDetails;
            Profiles.Add(newProfileDetails["id"].ToString(), newProfile);
        }

        public bool ProfileExists(string username, out string sessionId)
        {
            sessionId = null;
            foreach (var profile in Profiles.Values)
            {
                var info = profile.Info;
                var infoUsername = info["username"].ToString();
                if (info["username"].ToString() == username)
                {
                    sessionId = info["id"].ToString();
                    return true;
                }
            }

            return false;

        }

        public bool NameExists(string username)
        {
            foreach (var profile in Profiles.Values)
            {
                var info = profile.Info;
                var infoUsername = info["username"].ToString();
                if (info["username"].ToString() == username)
                    return true;
            }

            return false;
        }

        public class ProfileModel : DynamicObject
        {
            [JsonProperty("info")]
            public Dictionary<string, dynamic> Info = new Dictionary<string, dynamic>();

            public int AccountId => int.Parse(Info["id"].ToString());

            [JsonProperty("characters")]
            public Dictionary<string, Dictionary<string, dynamic>> Characters = new Dictionary<string, Dictionary<string, dynamic>>()
            {
                { "pmc", new Dictionary<string, dynamic>() },
                { "scav", new Dictionary<string, dynamic>() }
            };

            //public class ProfileCharacterModel
            //{
                
            //}
        }
    }
}
