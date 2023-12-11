using EFT;
using EFT.InventoryLogic;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.WebServer.BSG;
using System.Dynamic;
using MongoID = SIT.WebServer.BSG.MongoID;

namespace SIT.WebServer.Providers
{
    public class SaveProvider
    {
        public static Random Randomizer { get; } = new Random();

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
            File.WriteAllText(filePath, JsonConvert.SerializeObject(Profiles[sessionId], Formatting.Indented));
        }

        public ProfileModel LoadProfile(string sessionId)
        {
            if(sessionId == null) return null;

            var prof = Profiles[sessionId] as ProfileModel;
            //CleanIdsOfInventory(prof);

            return prof;
        }

        public Dictionary<string, object> GetPmcProfile(string sessionId)
        {
            var prof = Profiles[sessionId] as ProfileModel;
            var characters = prof.Characters;
            var pmcObject = characters["pmc"];

            // Add Arena
            if (!pmcObject.ContainsKey("Presets"))
                pmcObject.Add("Presets", new JObject());

            if (!pmcObject.ContainsKey("RankInfo"))
                pmcObject.Add("RankInfo", new JObject());

            var info = JObject.Parse(JsonConvert.SerializeObject(pmcObject["Info"]));
            if (!info.ContainsKey("RankInfo"))
            {
                //var rankInfo = new JObject();
                ////new RankInfo() { id = "None", points = new Dictionary<string, int>() };
                //rankInfo.Add(new { id = "None", points = new JObject() });
                //info.Add("RankInfo", rankInfo);
            }
            //if (pmcObject["Info"]. == null)
            //    pmcObject["Info"].RankInfo = new JObject();

            return pmcObject;
        }

        public Dictionary<string, object> GetScavProfile(string sessionId)
        {
            //var prof = Profiles[sessionId] as ProfileModel;
            //var characters = prof.Characters;
            //var scavObject = characters["scav"];
            //return scavObject;

            DatabaseProvider.TryLoadDatabaseFile("playerScav.json", out Dictionary<string, object> scav);
            scav["aid"] = sessionId;
            scav["id"] = "scav" + sessionId;
            scav["_id"] = "scav" + sessionId;
            JObject.FromObject(scav["Info"])["RegistrationDate"] = 1;
            return scav;
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

        public void CleanIdsOfInventory(ProfileModel profile)
        {
            if (profile == null) 
                return;

            if (profile.Characters.ContainsKey("pmc") && !profile.Characters["pmc"].ContainsKey("Inventory"))
                return;

            var inventory = profile.Characters["pmc"]["Inventory"];
            CleanIdsOfItems(inventory);

        }

        public void CleanIdsOfItems(JToken inventory)
        {
            var equipmentId = inventory["equipment"].ToString();
            var fastPanelId = inventory["fastPanel"].ToString();
            var hideoutAreaStashesId = inventory["hideoutAreaStashes"].ToString();
            var questRaidItemsId = inventory["questRaidItems"].ToString();
            var questStashItemsId = inventory["questStashItems"].ToString();
            var sortingTableId = inventory["sortingTable"].ToString();
            var stashId = inventory["stash"].ToString();

            Dictionary<string, string> remappedIds = new();
            Dictionary<string, string> allRemappedIds = new();

                remappedIds.Clear();
                Dictionary<string, int> IdCounts = new();


                foreach (var item in inventory["items"])
                {
                    if (item["_id"].ToString() == equipmentId)
                        continue;

                    if (item["_id"].ToString() == fastPanelId)
                        continue;

                    if (item["_id"].ToString() == hideoutAreaStashesId)
                        continue;

                    if (item["_id"].ToString() == questRaidItemsId)
                        continue;

                    if (item["_id"].ToString() == questStashItemsId)
                        continue;

                    if (item["_id"].ToString() == sortingTableId)
                        continue;

                    if (item["_id"].ToString() == stashId)
                        continue;

                    var oldId = item["_id"].ToString();
                    var newId = MongoID.Generate();
                    if (!remappedIds.ContainsKey(oldId))
                    {
                        remappedIds.Add(oldId, newId);
                        item["_id"] = newId;
                    }
                }

                foreach (var item in inventory["items"])
                {
                    var jO = item as JObject;
                    if (jO.ContainsKey("parentId"))
                    {
                        if (remappedIds.ContainsKey(jO["parentId"].ToString()))
                            jO["parentId"] = remappedIds[jO["parentId"].ToString()];

                    }
                }
          

        }

        public List<Items> GetProfileInventoryItems(string sessionId)
        {
            var pmcProfile = GetPmcProfile(sessionId);
            var inv = (JToken)pmcProfile["Inventory"];
            List<Items> items = new List<Items>();
            var invItems = (JArray)inv["items"];
            foreach (var item in invItems)
            {
                items.Add(item.ToObject<Items>());
            }

            return items;
        }

        public void ProcessProfileChanges(string sessionId, Changes changes)
        {
            if (!Profiles.ContainsKey(sessionId))
                return;

            var invItems = GetProfileInventoryItems(sessionId);

            if (changes.Stash != null)
            {
                if(changes.Stash.del != null)
                {

                    foreach (var d in changes.Stash.del)
                    {
                        var itemToRemoveWithChildren = FindAndReturnChildrenByItems(invItems, d._id);
                        foreach(var child in itemToRemoveWithChildren)
                        {
                            invItems.Remove(child);
                        }
                    }
                }
            }

            var prof = Profiles[sessionId] as ProfileModel;
            var pmcProfile = GetPmcProfile(sessionId);
            var inv = (JToken)pmcProfile["Inventory"];
            inv["items"] = JArray.FromObject(invItems);
            SaveProfile(sessionId, prof);

        }

        public List<Items> FindAndReturnChildrenByItems(List<Items> items, string itemId)
        {
            List<Items> result = new List<Items>(); 
            foreach (var item in items)
            {
                if(item.parentId == itemId)
                {
                    result.AddRange(FindAndReturnChildrenByItems(items, itemId));
                }
            }

            result.Add(items.Find(x => x._id == itemId));
            return result;
        }

        public class ProfileModel : DynamicObject
        {
            [JsonProperty("info")]
            public Dictionary<string, dynamic> Info = new Dictionary<string, dynamic>();

            public int AccountId => int.Parse(Info["aid"].ToString());

            [JsonProperty("characters")]
            public Dictionary<string, Dictionary<string, dynamic>> Characters { get; set; } = new Dictionary<string, Dictionary<string, dynamic>>()
            {
                { "pmc", new Dictionary<string, dynamic>() },
                { "scav", new Dictionary<string, dynamic>() }
            };

            [JsonProperty("suits")]
            public HashSet<string> Suits { get; set; } = new()
            {
                "5cde9ec17d6c8b04723cf479",
                "5cde9e957d6c8b0474535da7",
            };

            [JsonProperty("weaponbuilds")]
            public JObject WeaponBuilds { get; set; } = new();

            [JsonProperty("dialogues")]
            public JObject Dialogues { get; set; } = new();

            [JsonProperty("insurance")]
            public JArray Insurance { get; set; } = new();

            [JsonProperty("aki")]
            public JObject Aki { get; set; } = new();

            [JsonProperty("vitality")]
            public JObject Vitality { get; set; } = new();

            [JsonProperty("inraid")]
            public JObject InRaid { get; set; } = new();

            [JsonProperty("traderPurchases")]
            public JObject TraderPurchases { get; set; } = new();

            [JsonProperty("userbuilds")]
            public JObject UserBuilds { get; set; } = new();

            //public class ProfileCharacterModel
            //{

            //}
        }
    }
}
