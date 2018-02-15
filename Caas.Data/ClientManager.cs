using System;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

using Caas.Data.Models;
using Dapper;
using Dapper.Contrib.Extensions;

using Caas.Models.Interfaces;
using AutoMapper;

namespace Caas.Data
{
    /// <summary>
    /// Use <see cref="DatabaseContext"/> to manager <see cref="Client"/>
    /// </summary>
    public class ClientManager : DatabaseContext, IClientManager
    {
        /// <summary>
        /// Add new <see cref="Client"/> or update existing
        /// Automatically handles adding new attached <see cref="ClientType"/>, Parent <see cref="Client"/>, and <see cref="Config"/>
        /// </summary>
        /// <param name="client"></param>
        /// <returns>Updated <see cref="Client"/></returns>
        public async Task<Caas.Models.Client> AddOrUpdateClientAsync(Caas.Models.Client client)
        {
            var dbClient = Mapper.Map<Client>(client);
            using (var trans = Connection.BeginTransaction())
            {
                try
                {                    
                    dbClient = await AddOrUpdateClientInTransactionAsync(dbClient, trans);

                    trans.Commit();
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                }
            }

            return Mapper.Map<Caas.Models.Client>(dbClient);
        }

        /// <summary>
        /// Get a <see cref="Client"/> using the <see cref="Client.ClientId"/>
        /// </summary>
        /// <param name="clientId">The <see cref="Client.ClientId"/></param>
        /// <returns>The <see cref="Client"/></returns>
        /// <exception cref="ArgumentException">If <paramref name="clientId"/> equals 0</exception>
        public async Task<Caas.Models.Client> GetClientAsync(int clientId)
        {
            if (clientId == 0)
                throw new ArgumentException(nameof(clientId));

            return Mapper.Map<Caas.Models.Client>(await Connection.GetAsync<Client>(clientId));
        }

        /// <summary>
        /// Get a <see cref="Client"/> using the <see cref="Client.Identifier"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Client.Identifier"/></param>
        /// <returns>The <see cref="Client"/></returns>
        /// <exception cref="ArgumentException">If <paramref name="identifier"/> is null or empty</exception>
        public async Task<Caas.Models.Client> GetClientAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException(nameof(identifier));

            return Mapper.Map<Caas.Models.Client>(await Connection.QueryFirstOrDefault("SELECT * FROM [Client] WHERE [Identifier] = @Identifier", new { Identifier = identifier }));
        }

        /// <summary>
        /// Delete the <see cref="Client"/>
        /// </summary>
        /// <param name="client">The <see cref="Client"/> to delete</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If <paramref name="client"/> is null</exception>
        /// <exception cref="ArgumentException">If <see cref="Client.ClientId"/> equals 0</exception>
        public async Task DeleteClientAsync(Caas.Models.Client client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (client.ClientId == 0)
                throw new ArgumentException(nameof(client.ClientId));

            var dbClient = Mapper.Map<Client>(client);

            using (var trans = Connection.BeginTransaction())
            {
                try
                {
                    await Connection.QueryAsync("DELETE FROM [ConfigAssociation] WHERE [ClientId] = @ClientId", new { ClientId = client.ClientId }, trans);
                    await Connection.QueryAsync("UPDATE [Client] SET [ParentClientId] = NULL WHERE [ParentClientId] = @ClientId", new { ClientId = client.ClientId }, trans);
                    await Connection.DeleteAsync<Client>(dbClient, trans);

                    trans.Commit();
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                }
            }
        }

        private async Task<Client> AddOrUpdateClientInTransactionAsync(Client client, IDbTransaction transaction)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrEmpty(client.Identifier))
                throw new ArgumentNullException(nameof(client.Identifier));
            if (client.Type == null)
                throw new ArgumentNullException(nameof(client.Type));

            //Add the new type
            if (client.Type != null && client.Type.ClientTypeId == 0)
                client.Type.ClientTypeId = await Connection.InsertAsync(client.Type, transaction);

            //Check for parents
            if (client.Parent != null)
                client.Parent = await AddOrUpdateClientInTransactionAsync(client.Parent, transaction);

            //Create if not created time, otherwise update
            if (client.Created == DateTime.MinValue)
            {
                client.Created = DateTime.UtcNow;
                client.ClientId = await Connection.InsertAsync(client, transaction);
            }
            else
            {
                client.Updated = DateTime.UtcNow;
                await Connection.UpdateAsync(client, transaction);
            }

            //Check for configs
            if (client.Configurations?.Any() == true)
            {
                foreach (var config in client.Configurations)
                {
                    if(config.ConfigId == 0)
                    {
                        if (string.IsNullOrEmpty(config.Key))
                            throw new ArgumentNullException(nameof(config.Key));
                        if (string.IsNullOrEmpty(config.Value))
                            throw new ArgumentNullException(nameof(config.Value));
                        config.Created = DateTime.UtcNow;
                        config.ConfigId = await Connection.InsertAsync(config, transaction);
                    }
                    else
                    {
                        config.Updated = DateTime.UtcNow;
                        await Connection.UpdateAsync(config, transaction);
                    }

                    //Check if association exists, otherwise create
                    if (await Connection.QueryFirstAsync<int>("SELECT COUNT(*) FROM [ConfigAssocation] WHERE [ConfigId] = @ConfigId AND [ClientId] = @ClientId", new { ConfigId = config.ConfigId, ClientId = client.ClientId }) == 0)
                    {
                        var association = new ConfigAssociation();
                        association.ClientId = client.ClientId;
                        association.ConfigId = config.ConfigId;
                        association.Created = DateTime.UtcNow;
                        await Connection.InsertAsync(association, transaction);
                    }
                }
            }

            return client;
        }
    }
}
