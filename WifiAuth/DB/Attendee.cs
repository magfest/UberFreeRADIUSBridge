using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace WifiAuth.DB
{
    public class Attendee
    {

        private static readonly char delimiter = ';';

        private string _badgeLabels = "";
        private string _assignedDepartments = "";

        [JsonProperty("badge_num")]
        [Key]
        public string BadgeID { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("badge_type_label")]
        public string BadgeType { get; set; }

        [JsonProperty("ribbon_labels")]
        [NotMapped]
        public string[] BadgeLabels
        { 
            get { return _badgeLabels.Split(delimiter); }
            set { _badgeLabels = string.Join($"{delimiter}", value); }
        }

        [JsonProperty("zip_code")]
        public string ZipCode { get; set; }

        [JsonProperty("assigned_depts_labels")]
        [NotMapped]
        public string[] AssignedDepartments
        {
            get { return _assignedDepartments.Split(delimiter); }
            set { _assignedDepartments = string.Join($"{delimiter}", value); }
        }

        [JsonProperty("is_dept_head")]
        public bool IsDepartmentHead { get; set; }

        // Thanks to https://kimsereyblog.blogspot.com/2017/12/save-array-of-string-entityframework.html for this workaround
        // SQLite can't handle arrays of strings, and this'll work fine for what we're tring to do

        public Attendee()
        {

        }
    }
}
