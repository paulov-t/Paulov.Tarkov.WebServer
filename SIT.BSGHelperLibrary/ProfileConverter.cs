using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static EFT.Profile;

namespace SIT.BSGHelperLibrary
{
    public class ProfileConverter : JsonConverter<EFT.Profile>
    {
        public override bool CanRead => base.CanRead;
        public override bool CanWrite => base.CanWrite;

        public override void WriteJson(JsonWriter writer, EFT.Profile value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override EFT.Profile ReadJson(JsonReader reader, Type objectType, EFT.Profile existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject profile = null;

            try
            {
                profile = JObject.Load(reader);
            }
            catch (Exception ex)
            {
                return null;
            }
            if(existingValue == null)
                existingValue = new EFT.Profile();

            existingValue.Id = profile["_id"].ToString();
            existingValue.AccountId = profile["aid"].ToString();
            existingValue.BackendCounters = new System.Collections.Generic.Dictionary<string, BackendCounter>();
            var bc = new BonusController();
            foreach(var bonus in (JArray)profile["Bonuses"])
            {
                var ab = default(AbstractBonus);
                //bc.AddBonus(ab);
            }
            existingValue.Customization = BSGJsonHelpers.SITParseJson<Customization>(profile["Customization"].ToString());
            existingValue.Encyclopedia = BSGJsonHelpers.SITParseJson<Dictionary<string, bool>>(profile["Encyclopedia"].ToString());
            //existingValue.Experience = BSGJsonHelpers.SITParseJson<Dictionary<string, bool>>(profile["Encyclopedia"].ToString());
            //existingValue.Bonuses = new Bonu
            existingValue.Hideout = BSGJsonHelpers.SITParseJson<HideoutInfo>(profile["Hideout"].ToString());
            existingValue.Inventory = BSGJsonHelpers.SITParseJson<EFT.InventoryLogic.Inventory>(profile["Inventory"].ToString());
            //existingValue.InsuredItems = BSGJsonHelpers.SITParseJson<PreinsuredItem[]>(profile["WishList"].ToString());
            existingValue.TradersInfo = BSGJsonHelpers.SITParseJson<Dictionary<string, TraderInfo>>(profile["TradersInfo"].ToString());
            existingValue.WishList = BSGJsonHelpers.SITParseJson<string[]>(profile["WishList"].ToString());

            return existingValue;
        }
    }

    public class InventoryConverter : JsonConverter<EFT.InventoryLogic.Inventory>
    {
        public override Inventory ReadJson(JsonReader reader, Type objectType, Inventory existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject Inventory = JObject.Load(reader);
            if (existingValue == null)
                existingValue = new EFT.InventoryLogic.Inventory();

            // Equipment Template is a GClass. Lets get that dynamically.
            var equipmentConstructor = typeof(Equipment).GetConstructors().First();
            var paramType = equipmentConstructor.GetParameters()[1].ParameterType;
            var equipmentTemplate = Activator.CreateInstance(paramType);
            existingValue.Equipment = (Equipment)equipmentConstructor.Invoke(new object[] { Inventory["equipment"].ToString(), equipmentTemplate });
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, Inventory value, JsonSerializer serializer)
        {
        }

        public class LittleTemplate : ParentTemplate
        {
            public LittleTemplate()
            {

            }
        }
    }
}