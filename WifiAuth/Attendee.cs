using System;
using Newtonsoft.Json;

namespace WifiAuth
{
      public class Attendee
      {


            [JsonProperty("badge_num")]
            public string BadgeID;

            [JsonProperty("full_name")]
            public string FullName;

            [JsonProperty("badge_type_label")]
            public string BadgeType;

            [JsonProperty("ribbon_labels")]
            public string[] BadgeLabels;

            [JsonProperty("zip_code")]
            public string ZipCode;


            public Attendee()
            {

            }
      }
}
