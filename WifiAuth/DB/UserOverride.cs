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


            public UserOverride()
            {
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
