using System;
using System.Threading.Tasks;

using Caas.Data.Models;
using Caas.Models.Interfaces;
using Dapper;
using Dapper.Contrib.Extensions;
using AutoMapper;

namespace Caas.Data
{
    /// <summary>
    /// Use <see cref="DatabaseContext"/> to manage <see cref="ClientType"/>
    /// </summary>
    public class ClientTypeManager : DatabaseContext, IClientTypeManager
    {
        /// <summary>
        /// Add a new <see cref="ClientType"/>
        /// </summary>
        /// <param name="clientType">The <see cref="ClientType"/></param>
        /// <returns>The <see cref="ClientType"/> with updated values</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="clientType"/> or <see cref="ClientType.Name"/> is null or empty</exception>
        public async Task<Caas.Models.ClientType> AddClientTypeAsync(Caas.Models.ClientType clientType)
        {
            if (clientType == null)
                throw new ArgumentNullException(nameof(clientType));
            if (string.IsNullOrEmpty(clientType.Name))
                throw new ArgumentNullException(nameof(clientType.Name));

            var dbClientType = Mapper.Map<ClientType>(clientType);

            using (var trans = Connection.BeginTransaction())
            {
                try
                {
                    clientType.Created = DateTime.UtcNow;
                    dbClientType.Created = clientType.Created;
                    clientType.ClientTypeId = await Connection.InsertAsync(clientType, trans);

                    trans.Commit();
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                }
            }

            return clientType;
        }

        /// <summary>
        /// Get <see cref="ClientType"/> by <see cref="ClientType.ClientTypeId"/>
        /// </summary>
        /// <param name="clientTypeId">The <see cref="ClientType.ClientTypeId"/></param>
        /// <returns><see cref="ClientType"/> or null</returns>
        /// <exception cref="ArgumentException">If <paramref name="clientTypeId"/> equals 0</exception>
        public async Task<Caas.Models.ClientType> GetClientTypeAsync(int clientTypeId)
        {
            if (clientTypeId == 0)
                throw new ArgumentException(nameof(clientTypeId));

            return Mapper.Map<Caas.Models.ClientType>(await Connection.GetAsync<ClientType>(clientTypeId));
        }

        /// <summary>
        /// Get <see cref="ClientType"/> by <see cref="ClientType.Name"/>
        /// </summary>
        /// <param name="name">The <see cref="ClientType.Name"/></param>
        /// <returns><see cref="ClientType"/> or null</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="name"/> is null or empty</exception>
        public async Task<Caas.Models.ClientType> GetClientTypeAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            return Mapper.Map<Caas.Models.ClientType>(await Connection.QuerySingleOrDefaultAsync<ClientType>("SELECT * FROM [ClientType] WHERE [Name] = @Name", new { Name = name }));
        }
    }
}
