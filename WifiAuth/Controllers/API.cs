using System;
//using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WifiAuth.DB;
using System.Linq;
using System.Net.Http;

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

            int loggedEntryCount = _context.DeviceLogEntries.Select(e=>e.MACAddress).AsNoTracking().Count();

            return Ok(loggedEntryCount);
        }


        /// <summary>
        /// Get the top users, sorted by Unique MAC Address Count
        /// </summary>
        /// <param name="limit">Max number of records to return</param>
        /// <returns>Top users by unique MAC Address Count</returns>
        [HttpGet("GetTopUsers")]
        public IActionResult GetTopUsers(int limit=25)
        {

            var TopUsers = _context.DeviceLogEntries
                .GroupBy(e=>e.Login)
                .Select(x=> new {
                    UserID = x.Key,
                    Name = _context
                        .Attendees
                        .Where(a=>a.BadgeID == x.Key)
                        .Select(b=>b.FullName)
                        .First(),
                    Count = x.Count()})
                .OrderByDescending(d=>d.Count)
                .Take(limit)
                .AsNoTracking();

            return Ok(TopUsers);
        }

    }
}
