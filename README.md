**UberFreeRADIUSBridge** is an ASP.NET Core webapp that sits between [MAGFest's Ubersystem](https://github.com/magfest/ubersystem) and FreeRADIUS. It allows users to log into WiFi using their Badge Number and Zip code, if their badge level gives them access.

# Features
- Supports allowing users access based on their badge level (Ex: Staff and panelists get access; regular attendees do not)
- Temporarily caches responses from Uber to save API lookups
- SQLite backend for logging users/MACs, as well as supporting user overrides 
- Badge numbers can be force denied, force allowed, or force allowed with a custom password
- Handles multiple simultaneous auth attempts by running the ASP.NET Core Kestrel Websever
- Supports a static `laptop` login for event-provided devices that need to auth with the network
- Makes use of FreeRADIUS’s REST module

# Performance
We ran this bridge at MAGFest 2018 with roughly ~600 simultaneous WiFi clients peak and ~1,300 total connected devices, with no issues on the bridge side. Our biggest pain points were our wireless controller aggressively blocking users with too many failed auth attempts, and non-numerical zip codes.

# To Do
- Handle non-numerical zip codes -- right now, the code requires an exact case match. We should find some way to handle this better, as end users don't expect case to matter.
- Add WebUI to add in overrides, rather than requiring editing the SQLite file directly
- Have some form of administrative interface to show stats
- Document the FreeRADIUS setup needed to make use of the bridge.

# Want to run this?
Everything is in a functional, runable state, but documentation is lacking. Open a GitHub issue if you have any issues, and we'll take a look. We anticipate having better FreeRADIUS setup documentation once we have our rack powered back on post-event and we've caught up on sleep.

# Set Secrets
You'll need to set up [ASP.NET Core Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-2.2) to make this run. To do so, run the following with the correct info:
```
dotnet user-secrets set "LaptopPassword" "ThisIsntOurRealPassword"
dotnet user-secrets set "APIKey" "ffffffff-ffff-ffff-ffff-ffffffffffff"
dotnet user-secrets set "UberServer" "https://staging4.uber.magfest.org/uber/jsonrpc"
```

# Very Fast Demo
Build the project, enter some screts as given above, and start the project.

Run `curl -i  -H "Content-Type: application/json" http://localhost:5000/radius/user/laptop/mac/aa:bb:cc:dd:ee:ff`, which simulates a user named `laptop` making a login request. This should return, even if you don't have an UberServer or API key defined. (Note: This won't work in a browser, as the wrong `Content-Type` is sent.)

```
HTTP/1.1 200 OK
Date: Sat, 09 Nov 2019 06:59:59 GMT
Content-Type: application/json; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked

{"control:Cleartext-Password":"ThisIsntOurRealPassword","User-Name":"laptop - laptop","Tunnel-Private-Group-Id":0}
```
