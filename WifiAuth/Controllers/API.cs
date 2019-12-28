using System;
//using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WifiAuth.DB;
using System.Linq;
using System.Net.Http;
using static WifiAuth.Logic;

namespace WifiAuth.Controllers
{
    [Route("API/")]
    public class API : Controller
    {

        private readonly WifiAuthContext _context;

        public API(WifiAuthContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hi yes this is an API");
        }


        [HttpGet("GetMACsSeen")]
        public IActionResult GetMACsSeen()
        {

            int loggedEntryCount = _context.DeviceLogEntries.Select(e => e.MACAddress).AsNoTracking().Count();

            return Ok(loggedEntryCount);
        }


        /// <summary>
        /// Get the top users, sorted by Unique MAC Address Count
        /// </summary>
        /// <param name="limit">Max number of records to return</param>
        /// <returns>Top users by unique MAC Address Count</returns>
        [HttpGet("GetTopUsers")]
        public IActionResult GetTopUsers(int limit = 25)
        {

            var TopUsers = _context.DeviceLogEntries
                .GroupBy(e => e.Login)
                .Select(x => new
                {
                    UserID = x.Key,
                    Name = _context
                        .Attendees
                        .Where(a => a.BadgeID == x.Key)
                        .Select(b => b.FullName)
                        .First(),
                    Count = x.Count()
                })
                .OrderByDescending(d => d.Count)
                .Take(limit)
                .AsNoTracking();

            return Ok(TopUsers);
        }


        /// <summary>
        /// Looks up a User in both Uber and the Override Table
        /// </summary>
        /// <param name="BadgeNum">Badge number</param>
        /// <returns>Info about the BadgeNum, including if they're allowed, overrides, their zip, and more.</returns>
        [HttpGet("LookUpUser")]
        public IActionResult LookUpUser(string BadgeNum = "")
        {
            int badgenum;
            UberResponse rsp = null;

            // See if it's a barcode, and call Uber to get a badge number out of it 
            if (BadgeNum.StartsWith("~", StringComparison.CurrentCultureIgnoreCase))
            {
                rsp = Logic.LookUpBarcodeInUber(BadgeNum);

                if (rsp.ResponseStatus != 200)
                {
                    return NotFound(rsp.ResponseBody);
                }

            }
            else if (int.TryParse(BadgeNum, out badgenum))
            {

                rsp = Logic.LookUpBadgeNumberInUber(BadgeNum);

                if (rsp.ResponseStatus != 200)
                {
                    return NotFound(rsp.ResponseBody);
                }
            }
            else
            {
                return NotFound();
            }

            var overrideEntry = _context.UserOverrides.Find(BadgeNum);

            string OverrideType = "None";
            if (overrideEntry != null)
            {
                OverrideType = overrideEntry.Override.ToString();
            }

            var v = new
            {
                AllowedAccess = Logic.IsUserAllowedWiFiByDefault(rsp.user),
                rsp.user.ZipCode,
                OverrideType,
                BadgeNum = rsp.user.BadgeID,
                AttendeeObject = rsp.user,
            };
            return Ok(v);
        }



        /// <summary>
        /// Adds an override for a particular badge number
        /// </summary>
        /// <param name="data">BadgeNum to add a force override</param>
        /// <returns></returns>
        [HttpPost("AddOverride")]
        public IActionResult AddOverride([FromBody] OverrideContainer data)
        {

            int badgenum;
            if (!int.TryParse(data.BadgeNum, out badgenum))
            {
                return NotFound("Not a numerical badge.");
            }

            UberResponse rsp = Logic.LookUpBadgeNumberInUber(data.BadgeNum);

            if (rsp.ResponseStatus != 200)
            {
                return NotFound(rsp.ResponseBody);
            }

            var overrideEntry = _context.UserOverrides.Find(data.BadgeNum);

            // If there's already an entry in place, dive into it 
            if (overrideEntry != null)
            {
                // Bail if the override is already a higher level -- we don't want to remove additional access
                if (overrideEntry.Override == OverrideType.ForceToTechOps || overrideEntry.Override == OverrideType.ForceAllowWithPassword)
                {
                    return Forbid();
                }
                else
                {
                    // Update and commit the override
                    overrideEntry.Override = OverrideType.ForceAllow;
                    _context.UserOverrides.Update(overrideEntry);
                    _context.SaveChanges();
                    return Ok("Updated!");
                }

            }
            else
            {
                // No override exists, so add and commit it
                UserOverride or = new UserOverride(data.BadgeNum, OverrideType.ForceAllow);
                _context.UserOverrides.Add(or);
                _context.SaveChanges();
                return Ok("Added!");
            }

        }

    }

    /// <summary>
    /// Workaround to handle asp.net not being fond of taking primitives directly
    /// </summary>
    public class OverrideContainer
    {
        public readonly string BadgeNum;

        public OverrideContainer(string BadgeNum)
        {
            this.BadgeNum = BadgeNum;
        }
    }
}
