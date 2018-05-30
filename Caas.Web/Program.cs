using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Caas.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            using(var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<DatabaseContext>();
                try
                {
                    context.Database.Migrate();
                }
                catch(Exception) { /* Migrations might have an error for some, because I was learning EF and did it wrong */ }

                try
                {
                    SeedData.Initialize(services).Wait();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                    throw ex;
                }
            }

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://*:5000", "https://*:5001")
                .UseKestrel(options =>
                {
                    options.Limits.MaxConcurrentConnections = 100;
                    options.Limits.MaxConcurrentUpgradedConnections = 100;
                    options.Listen(IPAddress.Any, 5000);
                    /*
                     * Uncomment and replace with your certifcate to use https
                    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                    {
                        listenOptions.UseHttps("cert.pfx", "password")
                    });
                    */
                })
                .Build();
    }
}
