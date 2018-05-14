using System;

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
#if DEBUG
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
#else
				options.UseSqlServer(connString);
#endif
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
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "api",
                    template: "api/{controller}/{action}/{id?}");
            });
        }
    }
}
