using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Caas.Models;

namespace Caas.Web
{
	/// <summary>
	/// Database Manager for Caas
	/// </summary>
	public class DatabaseContext : IdentityDbContext<ApplicationUser>
	{
		/// <summary>
		/// Ctor
		/// Ensures database is created
		/// </summary>
		/// <param name="options"></param>
		public DatabaseContext(DbContextOptions<DatabaseContext> options)
			: base(options)
		{
			this.Database.EnsureCreated();
		}

        /// <summary>
        /// Get all <see cref="Client"/>s
        /// </summary>
        public DbSet<Client> Client { get; set; }

        /// <summary>
        /// Get all <see cref="ClientType"/>s
        /// </summary>
        public DbSet<ClientType> ClientType { get; set; }

        /// <summary>
        /// Get all <see cref="Config"/>s
        /// </summary>
        public DbSet<Config> Config { get; set; }

        /// <summary>
        /// Get all <see cref="ConfigAssociation"/>
        /// </summary>
        public DbSet<ConfigAssociation> ConfigAssociation { get; set; }

        /// <summary>
        /// Get all <see cref="CheckIn"/>s
        /// </summary>
        public DbSet<CheckIn> CheckIn { get; set; }
    }
}
