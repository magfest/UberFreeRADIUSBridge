using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WifiAuth.DB;
using Microsoft.Extensions.Hosting;

namespace WifiAuth
{
      public class Startup
      {

            // We'll retreive these from the Secrets Manager
            // See the Readme on how to set these
            public static string _laptopPassword { get; private set; }
            public static string _APIKey { get; private set; }
            public static string _UberAPIAddress { get; private set; }

            public Startup(IConfiguration configuration)
            {
                  Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                  services.AddMemoryCache();
                  services.AddMvc().AddNewtonsoftJson();
                  services.AddControllers();

                   services.AddDbContext<WifiAuthContext>(options =>
                                                         options.UseSqlite("Data Source=wifiauth.db"));

                  // Load settings from the Secret Manager
                  // TODO: This needs to bail if not set, rather than silently continuing and then erroring later
                  _laptopPassword = Configuration["LaptopPassword"];
                  _APIKey = Configuration["APIKey"];
                  _UberAPIAddress = Configuration["UberServer"];

            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app, IWebHostEnvironment env, WifiAuthContext context)
            {

                // Auto create/migrate the db on start
                context.Database.Migrate();

                if (env.IsDevelopment())
                  {
                        app.UseDeveloperExceptionPage();
                  }

                  //app.UseMvc();
                  app.UseRouting();
                  app.UseEndpoints(endpoints => {
                        endpoints.MapControllers();
                  });
            }
      }
}
