using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace Caas.Web
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var adminId = await EnsureUser(serviceProvider, "admin@example.com", "Password123");
        }

        static async Task<string> EnsureUser(IServiceProvider serviceProvider, string email, string password)
        {
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(email);
            if(user == null)
            {
                user = new ApplicationUser()
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, password);
            }

            return user.Id;
        }
    }
}
