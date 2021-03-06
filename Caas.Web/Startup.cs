﻿using System;
using System.Security.Claims;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Caas.Web
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
#if DEBUG || TEST
			Environment.SetEnvironmentVariable("INMEMORYCACHE_USE", "true", EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("CAAS_CREATECLIENTS", "true", EnvironmentVariableTarget.Process);
#endif

			var useInMemoryCache = Environment.GetEnvironmentVariable("INMEMORYCACHE_USE") ?? "true";
			if (useInMemoryCache == "true")
				services.AddMemoryCache();

			services.AddMvc();

			//For docker, set enviroment variables to configure the data source
			var hostname = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost\\SQLEXPRESS";
			var username = Environment.GetEnvironmentVariable("SQLSERVER_USER") ?? "sa";
			var password = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD") ?? "password";
			var connString = $"Data Source={hostname};Initial Catalog=Caas;User ID={username};Password={password}";

			services.AddDbContext<DatabaseContext>(options =>
			{
#if DEBUG
				options.UseSqlite("Data Source=configs.db");
#elif TEST
				options.UseSqlite("Data Source=tests.db");
#else
				options.UseSqlServer(connString);
#endif
			});

			//Add Authentication for Portal Management
			services.AddIdentity<ApplicationUser, IdentityRole>()
					.AddEntityFrameworkStores<DatabaseContext>()
			        .AddDefaultTokenProviders();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ValidAccount", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("ValidAccount", "true");
                });
            });

			services.Configure<IdentityOptions>(options =>
			{
				// Lockout settings
				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30); //TODO: Configurable
				options.Lockout.MaxFailedAccessAttempts = 10; //TODO: Configurable

                // User settings
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
			});

			services.ConfigureApplicationCookie(options =>
			{
				options.ExpireTimeSpan = TimeSpan.FromMinutes(30); //TODO: Configurable
                
				options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
			});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                loggerFactory.AddConsole(LogLevel.Debug);
            }
            else
                loggerFactory.AddEventSourceLogger();

			app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(
                    name: "api",
                    template: "api/{controller}/{action}/{id?}");
            });
        }
    }
}
