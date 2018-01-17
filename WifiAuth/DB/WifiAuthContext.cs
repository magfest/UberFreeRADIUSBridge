using System;
using Microsoft.EntityFrameworkCore;

namespace WifiAuth.DB
{
      public class WifiAuthContext : DbContext
      {

            public WifiAuthContext(DbContextOptions<WifiAuthContext> options) : base(options)
            {
            }

            public DbSet<DeviceLogEntry> DeviceLogEntries { get; set; }
            public DbSet<UserOverride> UserOverrides { get; set; }



      }
}
