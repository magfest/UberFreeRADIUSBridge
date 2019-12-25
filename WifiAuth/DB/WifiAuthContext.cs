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
            public DbSet<Attendee> Attendees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attendee>()
                .Property<string>("_badgeLabels")
                .HasField("_badgeLabels");

            modelBuilder.Entity<Attendee>()
                .Property<string>("_assignedDepartments")
                .HasField("_assignedDepartments");
        }



    }
}
