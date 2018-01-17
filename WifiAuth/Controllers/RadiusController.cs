using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WifiAuth.DB;

namespace WifiAuth.Controllers
{
      [Route("radius/")]
      public class RadiusController : Controller
      {
            private IMemoryCache _cache;
            private readonly WifiAuthContext _context;

            public RadiusController(IMemoryCache memoryCache, WifiAuthContext context)
            {
                  _cache = memoryCache;
                  _context = context;
            }




            // GET /
            [HttpGet]
            public IActionResult Get()
            {
                  String PostContent = @"{ ""method"":""config.info""}";


                  var httpClient = new HttpClient();
                  httpClient.DefaultRequestHeaders.Add("X-Auth-Token", Config.APIKey);

                  HttpContent EncodedPostContent = new StringContent(PostContent);
                  var response = httpClient.PostAsync(Config.UberServer, EncodedPostContent).Result;

                  String RspBody = response.Content.ReadAsStringAsync().Result;

                  Console.WriteLine(RspBody);

                  if (response.IsSuccessStatusCode)
                  {
                        return Ok(RspBody);
                  }
                  else
                  {
                        return StatusCode(500, RspBody);
                  }



            }


            // ${..connect_uri}/user/%{User-Name}/sessions/%{Acct-Unique-Session-ID}
            [HttpPost("user/{username}/sessions/{sessionID}")]
            [Produces("application/json")]
            public IActionResult GetAccounting(string username, string sessionID, [FromBody] dynamic content)
            {

                  Console.WriteLine(sessionID);
                  Console.WriteLine(content);


                  return Ok();

            }



            // POST /user/test/mac/08-d0-9f-ec-2a-f0%3AMAGFest%202018?action=authorize

            [HttpPost("user/{username}/mac/{callingStation?}")]
            [HttpGet("user/{username}/mac/{callingStation?}")]
            [Produces("application/json")]
            public async Task<IActionResult> GetAuthenticateAuthorize(string username, string callingStation, string action, [FromBody] dynamic content)
            {

                  // Make a "nice" version of the username for logging/display.
                  string formattedUsername = string.Format("[{0,-10}] ", username);

                  Console.WriteLine(formattedUsername + "Beginning to process login for " + username + " with MAC of " + callingStation);

                  // Make sure the request originates from localhost for security reasons
                  if (!(HttpContext.Connection.RemoteIpAddress.ToString() == "127.0.0.1" || HttpContext.Connection.RemoteIpAddress.ToString() == "::1"))
                  {
                        Console.WriteLine(formattedUsername + " /!\\ Request from " + HttpContext.Connection.RemoteIpAddress.ToString() + " denied.");
                        return StatusCode(401, "Invalid source IP");
                  }

                  // Make sure the username is either 'laptop' or an integer so we don't hammer Uber with invalid entries
                  int num;
                  if (!(username == "laptop" || int.TryParse(username, out num)))
                  {
                        Console.WriteLine(formattedUsername + "Invalid login username from " + username);
                        return StatusCode(500, "Invalid login paramater.");
                  }

                  // Verify that the `laptop` login is coming from a valid MAC
                  if (username == "laptop")
                  {
                        return ProcessLaptopLogin(callingStation, action);
                  }

                  // See if there's Override information
                  var overrideEntry = _context.UserOverrides.Find(username);

                  // Deny if there's an Override flag
                  if (null != overrideEntry && overrideEntry.Override == OverrideType.ForceDeny)
                  {
                        Console.WriteLine(formattedUsername + "Force deny override in place for " + username + " !");
                        return StatusCode(401, "Denied via override");
                  }

                  // Allow if there's an Override flag that returns a custom password
                  if (null != overrideEntry && overrideEntry.Override == OverrideType.ForceAllowWithPassword)
                  {
                        Console.WriteLine(formattedUsername + "Force allow override in place for " + username + " with custom password!");
                        LogUserLoginToDevice(username, callingStation);
                        return Json(new RADIUSResponse(overrideEntry.PasswordOverride, "Forced Allow", username));
                  }



                  // Start building the user
                  Attendee user;

                  // Look for cache key.
                  if (!_cache.TryGetValue(username, out user))
                  {

                        Console.WriteLine(formattedUsername + "Doing live lookup in Uber for user");

                        // It's not the most ideal way of building a JSON search...
                        String PostContent = @"{ ""method"":""attendee.lookup"", ""params"": [""" + username + @"""]}";

                        // Build the HTTP client and send it 
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("X-Auth-Token", Config.APIKey);
                        HttpContent EncodedPostContent = new StringContent(PostContent);
                        var response = httpClient.PostAsync(Config.UberServer, EncodedPostContent).Result;

                        String RspBody = response.Content.ReadAsStringAsync().Result;

                        // If Uber gives us an error, bail and pass it up
                        if (!response.IsSuccessStatusCode)
                        {
                              Console.WriteLine(formattedUsername + "!!! Uber returned an error.");
                              return StatusCode(500, RspBody);
                        }

                        //Console.WriteLine(RspBody);

                        // Parse out the JSON
                        dynamic json = JToken.Parse(RspBody);

                        // This is an RPC error, so return a Server Failure
                        if (null != json.error)
                        {
                              Console.WriteLine(formattedUsername + "!!! Uber returned an error.");
                              return StatusCode(500, RspBody);
                        }

                        // Probably a badge not found error.
                        if (null != json.result.error)
                        {
                              Console.WriteLine(formattedUsername + "!!! Uber returned a user not found error.");
                              return StatusCode(404, RspBody);
                        }

                        // Create an attendee
                        user = json.result.ToObject<Attendee>();

                        // Keep the attendee around for an hour
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                              .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                        // Save data in cache.
                        _cache.Set(username, user, cacheEntryOptions);
                  }


                  // Allow if there's an Override flag
                  bool forceAllow = false;
                  if (null != overrideEntry && overrideEntry.Override == OverrideType.ForceAllow)
                  {
                        forceAllow = true;
                  }

                  // Check to see if the Attendee is allowed to access the internet
                  if (user.BadgeType == "Staff" || user.BadgeType == "Guest" || user.BadgeLabels.Contains("Panelist") || user.BadgeLabels.Contains("Volunteer") || user.BadgeLabels.Contains("Shopkeep") || forceAllow)
                  {
                        if (forceAllow) { Console.WriteLine(formattedUsername + "Force allow override in place for " + username + " !"); }

                        Console.WriteLine(formattedUsername + "Signin allowed!");

                        LogUserLoginToDevice(username, callingStation);

                        return Json(new RADIUSResponse(user.ZipCode, user.FullName, username));
                  }
                  else
                  {
                        Console.WriteLine(formattedUsername + "Signin blocked -- badge flags not valid");
                        return StatusCode(401, new RejectedRadiusResponse("Sorry, your badge type is not allowed. Please see TechOps for assistance."));
                  }

            }

            private void LogUserLoginToDevice(string username, string callingStation)
            {
                  // Don't bother logging or looking up if no MAC is provided
                  if (string.IsNullOrEmpty(callingStation))
                  {
                        return;
                  }

                  string formattedUsername = string.Format("[{0,-10}] ", username);

                  // Get the number of entries for a mac/username combo
                  int loggedEntryCount = _context.DeviceLogEntries.Where(e => e.Login == username && e.MACAddress == callingStation).AsNoTracking().Count();

                  // If it's zero, add a new entry
                  if (loggedEntryCount == 0)
                  {
                        DeviceLogEntry logEntry = new DeviceLogEntry() { Login = username, MACAddress = callingStation, FirstSeen = DateTime.UtcNow };
                        _context.DeviceLogEntries.Add(logEntry);
                        _context.SaveChanges();
                  }

                  // Get count of MACs by username
                  int numMACsByUsername = _context.DeviceLogEntries.Where(e => e.Login == username).AsNoTracking().Count();
                  Console.WriteLine(formattedUsername + "--> " + username + " has logged into " + numMACsByUsername + " devices.");
            }


            private ActionResult ProcessLaptopLogin(string callingStation, string action)
            {
                  //string[] AllowedLaptopMACs = new string[] { "aa:bb:cc:dd:ee:ff", "d4:f4:6f:c2:14:ef", "90:2e:1c:2d:e7:95" };


                  //// Gracefully handle when we don't have a calling station (inner-tunnel)
                  //Boolean NoMAC = true;

                  ////Console.WriteLine(HttpContext.Request.Headers["X-FreeRADIUS-Server"]);

                  //if (HttpContext.Request.Headers["X-FreeRADIUS-Server"] != "inner-tunnel")
                  //{
                  //      // Clean up given MAC
                  //      callingStation = callingStation.ToLower();
                  //      callingStation = callingStation.Replace('-', ':');
                  //      NoMAC = false;
                  //}


                  //if (NoMAC || AllowedLaptopMACs.Contains(callingStation))
                  //{
                        Console.WriteLine("[ laptop  ] Allowing laptop login with MAC " + callingStation);
                        return Json(new RADIUSResponse(Config.laptopPassword, "laptop", "laptop"));
                  //}
                  //else
                  //{
                  //      Console.WriteLine("[ laptop  ] Rejecting laptop login with MAC " + callingStation);
                  //      return StatusCode(401, new RejectedRadiusResponse("Sorry, this MAC is not allowed to use this login"));
                  //}


            }


      }

      /// <summary>
      /// Rejected Radius response.
      /// </summary>
      public class RejectedRadiusResponse
      {

            [JsonProperty(PropertyName = "reply:Reply-Message")]
            public String ReplyMessage;

            public RejectedRadiusResponse(String reason)
            {
                  this.ReplyMessage = reason;
            }

      }


      /// <summary>
      /// Approved / accepted Radius response
      /// </summary>
      public class RADIUSResponse
      {

            [JsonProperty(PropertyName = "control:Cleartext-Password")]
            public String PlaintextPassword;

            [JsonProperty(PropertyName = "User-Name")]
            public String Username;

            /// <summary>
            /// Creates a successful Radius Response 
            /// </summary>
            /// <param name="PlaintextPassword">Plaintext password</param>
            /// <param name="RealName">Real name</param>
            /// <param name="BadgeNumber">Badge number</param>
            public RADIUSResponse(String PlaintextPassword, String RealName, string BadgeNumber)
            {
                  this.PlaintextPassword = PlaintextPassword;

                  // Whatever this is set to will be returned to FreeRadius / WLC as the username
                  this.Username = RealName + " - " + BadgeNumber;
            }

      }


      /// <summary>
      /// Config paramaters. You'll need to change these for anything to work.
      /// </summary>
      public static class Config
      {

            public static string APIKey = "ffffffff-ffff-ffff-ffff-ffffffffffff";

            public static Uri UberServer = new Uri("https://staging4.uber.magfest.org/uber/jsonrpc");

            public static string laptopPassword = "ThisIsntOurRealPassword";

      }






}
