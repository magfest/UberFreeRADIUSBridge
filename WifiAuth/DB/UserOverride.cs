using System;
using System.ComponentModel.DataAnnotations;

namespace WifiAuth.DB
{
    public class UserOverride
    {

        [Key]
        public string Login { get; set; }

        public OverrideType Override { get; set; }

        public string PasswordOverride { get; set; }


        public UserOverride(string Login, OverrideType Override, string PasswordOverride = "")
        {
            this.Login = Login;
            this.Override = Override;
            this.PasswordOverride = PasswordOverride;
        }
    }

    public enum OverrideType
    {
        NoOverride = 0,
        ForceAllow = 1,
        ForceDeny = 2,
        ForceAllowWithPassword = 3,
        ForceToTechOps = 4
    }

}
