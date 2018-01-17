using System;
namespace WifiAuth.DB
{
      public class DeviceLogEntry
      {


            public int DeviceLogEntryID { get; set; } 

            public string MACAddress { get; set; } 

            public string Login { get; set; } 

            public DateTime FirstSeen { get; set; } 

            public DeviceLogEntry()
            {
            }
      }
}
