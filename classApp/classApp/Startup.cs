using classApp.DataAccess;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace classApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment host)
        {
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                host.ContentRootPath + "\\cloudcomputingclass-377007-baf039d70271.json");
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string project = Configuration["project"];

            services.AddGoogleErrorReportingForAspNetCore(new ErrorReportingServiceOptions
            {
                // Replace ProjectId with your Google Cloud Project ID.
                ProjectId = project,
                // Replace Service with a name or identifier for the service.
                ServiceName = "MainWebApp",
                // Replace Version with a version for the service.
                Version = "1"
            });


            //oath_secretKey
            // Create the client.
            SecretManagerServiceClient client = SecretManagerServiceClient.Create();

            // Build the resource name.
            SecretVersionName secretVersionName = new SecretVersionName(project, "oath_secretKey", "1");

            // Call the API.
            AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);

            // Convert the payload to a string. Payloads are bytes by default.
            String payload = result.Payload.Data.ToStringUtf8();
            var key = JObject.Parse(payload);
            string secretKey = key["Authentication:Google:ClientSecret"].ToString();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogle(options =>
                {
                    options.ClientId = "500567993145-9a66bv7rrqfodumr68fduqe2ijkmp91h.apps.googleusercontent.com";
                    options.ClientSecret = "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";
                });

            services.AddControllersWithViews();

            //Dependency Injection
            services.AddScoped<FirestoreVideosRepository>(provider=>new FirestoreVideosRepository(project));
            services.AddScoped<RedisCacheMenusRepository>(provider=>new RedisCacheMenusRepository(Configuration["ConnectionString"]));
            services.AddScoped<PubsubTranscriptionsRepository>(provider=>new PubsubTranscriptionsRepository(project));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
