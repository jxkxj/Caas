using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Caas.Data.Models;
using Caas.Models.Interfaces;
using Dapper;
using Dapper.Contrib.Extensions;
using AutoMapper;

namespace Caas.Data
{
    /// <summary>
    /// Use <see cref="DatabaseContext"/> to manage <see cref="Config"/>
    /// </summary>
    public class ConfigManager : DatabaseContext, IConfigManager
    {
        /// <summary>
        /// Add a new <see cref="Config"/>
        /// </summary>
        /// <param name="config">The <see cref="Config"/></param>
        /// <returns><see cref="Config"/> with updated values</returns>
        /// <exception cref="ArgumentNullException">If <see cref="Config"/>, <see cref="Config.Key"/>, <see cref="Config.Value"/> is null</exception>
        public async Task<Caas.Models.Config> AddConfigAsync(Caas.Models.Config config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.Key))
                throw new ArgumentNullException(nameof(config.Key));
            if (string.IsNullOrEmpty(config.Value))
                throw new ArgumentNullException(nameof(config.Value));

            var dbConfig = Mapper.Map<Config>(config);

            using (var trans = Connection.BeginTransaction())
            {
                try
                {
                    config.Created = DateTime.UtcNow;
                    dbConfig.Created = config.Created;
                    config.ConfigId = await Connection.InsertAsync(dbConfig, trans);

                    trans.Commit();
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                }
            }

            return config;
        }

        /// <summary>
        /// Update an existing <see cref="Config"/>
        /// </summary>
        /// <param name="config">The <see cref="Config"/></param>
        /// <returns><see cref="Config"/> with updated values</returns>
        /// <exception cref="ArgumentNullException">If <see cref="Config"/>, <see cref="Config.Key"/>, or <see cref="Config.Value"/> is null</exception>
        /// <exception cref="ArgumentException">If <see cref="Config.ConfigId"/> equals 0/exception>
        public async Task<Caas.Models.Config> UpdateConfigAsync(Caas.Models.Config config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.ConfigId == 0)
                throw new ArgumentException(nameof(config.ConfigId));
            if (string.IsNullOrEmpty(config.Key))
                throw new ArgumentNullException(nameof(config.Key));
            if (string.IsNullOrEmpty(config.Value))
                throw new ArgumentNullException(nameof(config.Value));

            var dbConfig = Mapper.Map<Config>(config);

            using (var trans = Connection.BeginTransaction())
            {
                try
                {
                    config.Updated = DateTime.UtcNow;
                    dbConfig.Updated = config.Updated;
                    await Connection.UpdateAsync(config, trans);

                    trans.Commit();
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                }
            }

            return config;
        }

        /// <summary>
        /// Get a <see cref="Config"/> by <see cref="Config.ConfigId"/>
        /// </summary>
        /// <param name="configId">The <see cref="Config.ConfigId"/></param>
        /// <returns><see cref="Config"/> matching <paramref name="configId"/></returns>
        /// <exception cref="ArgumentException">If <paramref name="configId"/> equals 0</exception>
        public async Task<Caas.Models.Config> GetConfigAsync(int configId)
        {
            if (configId == 0)
                throw new ArgumentException(nameof(configId));
            return Mapper.Map<Caas.Models.Config>(await Connection.QuerySingleOrDefaultAsync<Config>("SELECT * FROM [Config] WHERE [ConfigId] = @Id", new { Id = configId }));
        }

        /// <summary>
        /// Get a <see cref="Config"/> by <see cref="Config.Key"/>
        /// </summary>
        /// <param name="key">The <see cref="Config.Key"/></param>
        /// <returns><see cref="Config"/> matching <paramref name="key"/></returns>
        /// <exception cref="ArgumentNullException">If <see cref="key"/> is null or empty</exception>
        public async Task<Caas.Models.Config> GetConfigAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            return Mapper.Map<Caas.Models.Config>(await Connection.QuerySingleOrDefaultAsync<Config>("SELECT * FROM [Config] WHERE [Key] = @Key", new { Key = key }));
        }

        /// <summary>
        /// Get all <see cref="Config"/>
        /// </summary>
        /// <returns>All <see cref="Config"/></returns>
        public async Task<IEnumerable<Caas.Models.Config>> GetAllConfigsAsync() => (await Connection.GetAllAsync<Config>()).Select(c => Mapper.Map<Caas.Models.Config>(c));

        /// <summary>
        /// Get a <see cref="Config"/> for a <see cref="Client"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <param name="key">The <see cref="Config.Key"/></param>
        /// <returns>The <see cref="Config"/></returns>
        /// <exception cref="ArgumentNullException">If <paramref name="identifier"/>,<paramref name="key"/>, or <paramref name="type"/> is null or empty</exception>
        public async Task<Caas.Models.Config> GetConfigForClientAsync(string identifier, string type, string key)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            Client client = await Connection.QueryFirstOrDefaultAsync<Client>("SELECT c.* FROM [Client] c INNER JOIN [ClientType] ct ON c.[ClientTypeId] = ct.[ClientTypeId] WHERE c.[Identifier] = @Identifer AND ct.[Name] = @Type", new { Identifier = identifier, @Type = type });

            //No client
            if (client == null)
                return null;

            return Mapper.Map<Caas.Models.Config>(await Connection.QueryFirstOrDefaultAsync<Config>(@"SELECT c.* FROM [Config] c INNER JOIN [ConfigAssocation] ca ON c.[ConfigId] = ca.[ConfigId] WHERE ca.[ClientId] = @ClientId AND c.[Key] = @Key",
                new { ClientId = client.ClientId, Key = key }));
        }

        /// <summary>
        /// Get all <see cref="Config"/> for a <see cref="Client"/>
        /// </summary>
        /// <param name="clientId">The <see cref="Client.ClientId"/></param>
        /// <returns>All <see cref="Config"/> for <see cref="Client"/></returns>
        /// <exception cref="ArgumentException">If <paramref name="clientId"/> equals 0</exception>
        public async Task<IEnumerable<Caas.Models.Config>> GetAllForClientAsync(int clientId)
        {
            if (clientId == 0)
                throw new ArgumentException(nameof(clientId));
            return (await Connection.QueryAsync<Config>("SELECT c.* FROM [Config] c INNER JOIN [ConfigAssocation] ca ON c.[ConfigId] = ca.[ConfigId] WHERE ca.[ClientId] = @ClientId", new { ClientId = clientId })).Select(c => Mapper.Map<Caas.Models.Config>(c));
        }

        /// <summary>
        /// Get all <see cref="Config"/> for a <see cref="Client"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <returns>All <see cref="Config"/> for a <see cref="Client"/></returns>
        /// <exception cref="ArgumentNullException">If <paramref name="identifier"/> or <paramref name="type"/> is null or empty</exception>
        public async Task<IEnumerable<Caas.Models.Config>> GetAllForClientAsync(string identifier, string type)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentNullException(nameof(identifier));
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            Client client = await Connection.QueryFirstOrDefaultAsync<Client>("SELECT c.* FROM [Client] c INNER JOIN [ClientType] ct ON c.[ClientTypeId] = ct.[ClientTypeId] WHERE c.[Identifier] = @Identifier AND ct.[Name] = @Type"
                , new { Identifier = identifier, @Type = type });

            if (client == null)
                return null;

            return (await Connection.QueryAsync<Config>("SELECT * FROM [Config] c INNER JOIN [ConfigAssociation] ca ON c.[ConfigId] = ca.[ConfigId] WHERE ca.[ClientId] = @ClientId", client)).Select(c => Mapper.Map<Caas.Models.Config>(c));
        }

        ///<summary>
        /// Delete a <see cref="Config"/>
        /// </summary>
        /// <param name="config">The <see cref="Config"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If <paramref name="config"/> is null</exception>
        /// <exception cref="ArgumentException">If <see cref="Config.ConfigId"/> equals 0</exception>
        public async Task DeleteConfigAsync(Caas.Models.Config config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.ConfigId == 0)
                throw new ArgumentException(nameof(config.ConfigId));

            var dbConfig = Mapper.Map<Config>(config);

            using (var trans = Connection.BeginTransaction())
            {
                try
                {
                    await Connection.DeleteAsync(dbConfig, trans);

                    trans.Commit();
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                }
            }
        }
    }
}
