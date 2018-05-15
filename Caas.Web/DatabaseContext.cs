using System;
using Microsoft.EntityFrameworkCore;

using Caas.Models;

namespace Caas.Web
{
	/// <summary>
	/// Database Manager for Caas
	/// </summary>
	public class DatabaseContext : DbContext
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
        /// Get all <see cref="Clients"/>
        /// </summary>
        public DbSet<Client> Client { get; set; }

        /// <summary>
        /// Get all <see cref="ClientType"/>
        /// </summary>
        public DbSet<ClientType> ClientType { get; set; }

        /// <summary>
        /// Get all <see cref="Config"/>
        /// </summary>
        public DbSet<Config> Config { get; set; }

        /// <summary>
        /// Get all <see cref="ConfigAssociation"/>
        /// </summary>
        public DbSet<ConfigAssociation> ConfigAssociation { get; set; }

        /// <summary>
        /// Get all <see cref="CheckIn"/>
        /// </summary>
        public DbSet<CheckIn> CheckIn { get; set; }
    }
}
