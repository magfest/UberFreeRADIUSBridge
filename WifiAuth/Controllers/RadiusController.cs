using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WifiAuth.DB;
using static WifiAuth.Logic;

namespace WifiAuth.Controllers
{
    [Route("radius/")]
    public class RadiusController : Controller
    {
        private readonly WifiAuthContext _context;

        public RadiusController(WifiAuthContext context)
        {
            _context = context;
        }


        // GET /
        [HttpGet]
        public IActionResult Get()
        {
            String PostContent = @"{ ""method"":""config.info""}";


            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Auth-Token", Startup._APIKey);

            HttpContent EncodedPostContent = new StringContent(PostContent);
            var response = httpClient.PostAsync(Startup._UberAPIAddress, EncodedPostContent).Result;

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

            Console.WriteLine("========== Accounting ==========");
            Console.WriteLine($"Username:  {username}");
            Console.WriteLine($"SessionID: {sessionID}");
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
            if (!(username.StartsWith("laptop", StringComparison.CurrentCultureIgnoreCase) || int.TryParse(username, out num)))
            {
                Console.WriteLine(formattedUsername + "Invalid login username from " + username);
                return StatusCode(500, "Invalid login paramater.");
            }

            // Verify that the `laptop(*)` login is coming from a valid MAC
            if (username.StartsWith("laptop", StringComparison.CurrentCultureIgnoreCase))
            {
                return ProcessLaptopLogin(username, callingStation, action);
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
            if (!GetUserFromLocalAttendeeList(username, out user))
            {
                // Do a lookup in Uber
                UberResponse rsp = Logic.LookUpInUber(username);
                if(rsp.ResponseStatus != 200)
                {
                    return StatusCode(rsp.ResponseStatus, rsp.ResponseBody);
                }

                // Insert it into the local DB
                InsertUserFromLocalAttendeeList(rsp.user);
            }


            // Allow if there's an Override flag
            bool forceAllow = false;
            if (null != overrideEntry && overrideEntry.Override == OverrideType.ForceAllow)
            {
                forceAllow = true;
            }

            // Check to see if the Attendee is allowed to access the internet
            if (Logic.IsUserAllowedWiFiByDefault(user) || forceAllow)
            {
                if (forceAllow) { Console.WriteLine(formattedUsername + "Force allow override in place for " + username + " !"); }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(formattedUsername + "Signin allowed for " + user.FullName + "!");
                Console.ResetColor();

                LogUserLoginToDevice(username, callingStation);

                // We ToLower() the "zip" to provide consistant handling of non-numerical postal codes
                if ((user.AssignedDepartments.Contains("Tech Ops") && user.IsDepartmentHead) || (null != overrideEntry && overrideEntry.Override == OverrideType.ForceToTechOps))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("[      --> ] TechOps Override!");
                    Console.ResetColor();
                    return Json(new RADIUSResponse(user.ZipCode.ToLower(), user.FullName, username, 12, true));
                }
                else
                {
                    return Json(new RADIUSResponse(user.ZipCode.ToLower(), user.FullName, username));
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(formattedUsername + "Signin blocked -- badge flags not valid");
                Console.ResetColor();
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
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(formattedUsername + "--> " + username + " has logged into " + numMACsByUsername + " devices.");
            Console.ResetColor();
        }


        private ActionResult ProcessLaptopLogin(string username, string callingStation, string action)
        {
            Console.WriteLine("[ laptop  ] Allowing laptop login with MAC " + callingStation);
            return Json(new RADIUSResponse(Startup._laptopPassword, username, "laptop"));
        }


        /// <summary>
        /// Tries to return a User from Uber with a given badge number
        /// </summary>
        /// <param name="username">The Badge Number of the user</param>
        /// <param name="attendee">The Attendee, if it exists, from the DB</param>
        /// <returns>True if the user was found in the DB</returns>
        private bool GetUserFromLocalAttendeeList(string username, out Attendee attendee)
        {
            attendee = new Attendee();

            if (_context.Attendees.Where(e => e.BadgeID == username).AsNoTracking().Count() >= 1)
            {
                attendee = _context.Attendees.Where(e => e.BadgeID == username).AsNoTracking().First();
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Inserts a User from Uber into the DB
        /// </summary>
        /// <param name="attendee"></param>
        private void InsertUserFromLocalAttendeeList(Attendee attendee)
        {
            Console.WriteLine($"[ Attende ] - Inserting {attendee.BadgeID} - {attendee.FullName} into the local DB.");
            _context.Attendees.Add(attendee);
            _context.SaveChanges();
        }

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

    [JsonProperty(PropertyName = "Tunnel-Type", NullValueHandling = NullValueHandling.Ignore)]
    public string tunnelType;

    [JsonProperty(PropertyName = "Tunnel-Medium-Type", NullValueHandling = NullValueHandling.Ignore)]
    public string tunnelMediumType;

    [JsonProperty(PropertyName = "Tunnel-Private-Group-Id", NullValueHandling = NullValueHandling.Ignore)]
    public int tunnelPrivateGroupID;

    //[JsonProperty(PropertyName = "Airespace-QOS-Level")]
    //public int qosLevel = 2;


    /// <summary>
    /// Creates a successful Radius Response
    /// </summary>
    /// <param name="PlaintextPassword">Plaintext password</param>
    /// <param name="RealName">Real name</param>
    /// <param name="BadgeNumber">Badge number</param>
    public RADIUSResponse(String PlaintextPassword, String RealName, string BadgeNumber, int? VLAN = null, bool? AllowUnlimitedBandwidth = null)
    {
        this.PlaintextPassword = PlaintextPassword;

        // Whatever this is set to will be returned to FreeRadius / WLC as the username
        this.Username = RealName + " - " + BadgeNumber;

        // If we get a VLAN, set up the other required items
        if (VLAN != null)
        {
            tunnelType = "VLAN";
            tunnelMediumType = "IEEE-802";
            tunnelPrivateGroupID = (int)VLAN;
        }
    }



}
