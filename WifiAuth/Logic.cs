using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using WifiAuth.DB;

namespace WifiAuth
{
    /// <summary>
    /// Shared logic between the API and the Radius Controllers
    /// </summary>
    public static class Logic
    {

        /// <summary>
        /// Business logic to determine if a user should get WiFi Access by default
        /// </summary>
        /// <param name="user">User obect to process</param>
        /// <returns>Should the user be allowed WiFi Access</returns>
        public static bool IsUserAllowedWiFiByDefault(Attendee user)
        {
            if (user.BadgeType == "Staff" ||
                user.BadgeType == "Contractor" ||
                user.BadgeType == "Guest" ||
                user.BadgeLabels.Contains("Panelist") ||
                user.BadgeLabels.Contains("Volunteer") ||
                user.BadgeLabels.Contains("Shopkeep"))
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        /// <summary>
        /// Looks up a badge number in Uber, and returns a wrapper with the status and the User, if found
        /// </summary>
        /// <param name="BadgeNumber">Badge Number of the user</param>
        /// <returns>Wrapped Response Status, Response Body, and User as an Attendee Object</returns>
        public static UberResponse LookUpInUber(string BadgeNumber)
        {
            // TODO: Console writing in a helper function isn't ideal.

            string formattedUsername = string.Format("[{0,-10}] ", BadgeNumber);
            Console.WriteLine(formattedUsername + "Doing live lookup in Uber for user");

            // It's not the most ideal way of building a JSON search...
            String PostContent = @"{ ""method"":""attendee.lookup"", ""params"": [""" + BadgeNumber + @""", ""full""]}";

            // Build the HTTP client and send it
            var httpClient = new HttpClient();

            // Fail fast to prevent all our threads from being consumed if Uber is having problems
            httpClient.Timeout = new TimeSpan(0, 0, 5);

            httpClient.DefaultRequestHeaders.Add("X-Auth-Token", Startup._APIKey);
            HttpContent EncodedPostContent = new StringContent(PostContent);
            var response = httpClient.PostAsync(Startup._UberAPIAddress, EncodedPostContent).Result;

            String RspBody = response.Content.ReadAsStringAsync().Result;

            // If Uber gives us an error, bail and pass it up
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(formattedUsername + "!!! Uber returned an error.");
                return new UberResponse(500, RspBody, null);
            }

            // Parse out the JSON
            dynamic json = JToken.Parse(RspBody);

            // This is an RPC error, so return a Server Failure
            if (null != json.error)
            {
                Console.WriteLine(formattedUsername + "!!! Uber returned an error.");
                return new UberResponse(500, RspBody, null);
            }

            // Probably a badge not found error.
            if (null != json.result.error)
            {
                Console.WriteLine(formattedUsername + "!!! Uber returned a user not found error.");
                return new UberResponse(404, RspBody, null);
            }

            // Create an attendee
            Attendee user = json.result.ToObject<Attendee>();

            return new UberResponse(200, RspBody, user);
        }

        /// <summary>
        /// Wrapper for Uber Responses
        /// </summary>
        public class UberResponse
        {
            public readonly int ResponseStatus;
            public readonly string ResponseBody;
            public readonly Attendee user;

            public UberResponse(int ResponseStatus, string ResponseBody, Attendee user)
            {
                this.ResponseStatus = ResponseStatus;
                this.ResponseBody = ResponseBody;
                this.user = user;
            }
        }

    }
}