using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

using Caas.Models;

namespace Caas.Web.Apis
{
    /// <summary>
    /// Manage getting all <see cref="Config"/> for <see cref="Client"/>
    /// </summary>
    [Produces("application/json")]
    [AllowAnonymous]
    public class ConfigController : Controller
    {
        private readonly DatabaseContext _context;

        private readonly IMemoryCache _cache;

        /// <summary>
        /// Basic Constructor
        /// </summary>
        /// <param name="context"><see cref="DatabaseContext"/></param>
        /// <param name="cache"><see cref="IMemoryCache"/></param>
        public ConfigController(DatabaseContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private bool CheckInRequest(string identifier, string type, string extraData = null)
        {
            Client client;

            //Check cache
            if (!_cache.TryGetValue(string.Format(CacheKeys.CLIENT_KEY, identifier, type), out client))
            {
                client = _context.Client.FirstOrDefault(c => c.Identifier == identifier && c.ClientType.Name == type);

                if (client == null && Environment.GetEnvironmentVariable("CAAS_CREATECLIENTS") == "true")
                {
                    var clientType = _context.ClientType.FirstOrDefault(c => c.Name == type);

                    if (clientType == null)
                    {
                        clientType = _context.ClientType.Add(new ClientType()
                        {
                            Name = type,
                            Created = DateTime.UtcNow
                        }).Entity;
                    }

                    client = _context.Client.Add(new Client()
                    {
                        ClientType = clientType,
                        Created = DateTime.UtcNow,
                        Identifier = identifier
                    }).Entity;

                    _context.SaveChanges();

                    //Add to cache
                    _cache.Set(string.Format(CacheKeys.CLIENT_KEY, identifier, type), client, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
                }
                else if (Environment.GetEnvironmentVariable("CAAS_CREATECLIENTS") != "true")
                    return false;
            }

            //Add Check In
            _context.CheckIn.Add(new Caas.Models.CheckIn()
            {
                ClientId = client.ClientId,
                ExtraData = extraData,
                CheckInTime = DateTime.UtcNow
            });

            _context.SaveChanges();

            return true;
        }

        /// <summary>
        /// Get a specific <see cref="Config"/> by <see cref="Config.Key"/> for a <see cref="Client"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <param name="key">The <see cref="Config.Key"/></param>
        /// <returns>The <see cref="Config"/> or null</returns>
        [HttpGet]
        public IActionResult GetConfigForClient(string identifier, string type, string key)
        {
            //Check in the client first
            CheckInRequest(identifier, type);

            //Look in cache first
            if (_cache.TryGetValue<Config>(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, identifier, type, key), out Config cacheConfig))
                return Ok(cacheConfig);

            Client client;

            if (!_cache.TryGetValue(string.Format(CacheKeys.CLIENT_KEY, identifier, type), out client))
            {
                client = _context.Client.FirstOrDefault(c => c.Identifier == identifier && c.ClientType.Name == type);
                _cache.Set(string.Format(CacheKeys.CLIENT_KEY, identifier, type), client, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }

            if (client == null)
                return NoContent();

            var configAssociation = _context.ConfigAssociation.Include(c => c.Config).Where(c => c.ClientId == client.ClientId && c.Config.Key == key).FirstOrDefault();
            if (configAssociation == null)
                return NoContent();
            else
            {
                var config = configAssociation.Config;
                if (!string.IsNullOrEmpty(configAssociation.Value))
                    config.Value = configAssociation.Value;
                _cache.Set(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, identifier, type, key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
                return Ok(config);
            }
        }

        /// <summary>
        /// Get a specific <see cref="Config"/> by <see cref="Config.Key"/>
        /// </summary>
        /// <param name="key">The <see cref="Config.Key"/></param>
        /// <returns>The <see cref="Config"/> or null</returns>
        [HttpGet]
        public IActionResult GetConfig(string key)
        {
            //Look in cache first
            if (_cache.TryGetValue(string.Format(CacheKeys.CONFIG_KEY, key), out Config cacheConfig))
                return Ok(cacheConfig);

            var config = _context.Config.FirstOrDefault(c => c.Key == key);

            if (config != null)
            {
                _cache.Set(string.Format(CacheKeys.CONFIG_KEY, key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
                return Ok(config);
            }
            else
                return NoContent();
        }

        /// <summary>
        /// Get all <see cref="Config"/>
        /// </summary>
        /// <returns>All <see cref="Config"/></returns>
        [HttpGet]
        public IActionResult GetAllConfigs() => Ok(_context.Config.ToList());

        /// <summary>
        /// Get all <see cref="Config"/> for a <see cref="Client"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <returns>All <see cref="Config"/> or null</returns>
        [HttpGet]
        public IActionResult GetAllConfigsForClient(string identifier, string type)
        {
            //Check in the client first
            CheckInRequest(identifier, type);

            Client client;

            if (!_cache.TryGetValue(string.Format(CacheKeys.CLIENT_KEY, identifier, type), out client))
            {
                client = _context.Client.FirstOrDefault(c => c.Identifier == identifier && c.ClientType.Name == type);
                _cache.Set(string.Format(CacheKeys.CLIENT_KEY, identifier, type), client, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }

            if (client == null)
                return NoContent();

            var configAssociations = _context.ConfigAssociation.Include(c => c.Config).Where(c => c.ClientId == client.ClientId);

            var configs = new List<Config>();
            foreach(var configAssociation in configAssociations)
            {
                var config = configAssociation.Config;
                //If association has a different value, use that instead of the default
                if (!string.IsNullOrEmpty(configAssociation.Value))
                    config.Value = configAssociation.Value;
                configs.Add(config);
            }

            return Ok(configs);
        }

        /// <summary>
        /// Allow a <see cref="Client"/> to check in
        /// Store a new <see cref="Models.CheckIn"/> record
        /// </summary>
        /// <param name="checkIn">The <see cref="Models.CheckIn"/> with at least the <see cref="Client.Identifier"/> and <see cref="ClientType.Name"/></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CheckIn([FromBody]CheckIn checkIn)
        {
            if (checkIn == null)
                return BadRequest();
            if (checkIn.Client == null)
                return BadRequest();
            if (string.IsNullOrEmpty(checkIn.Client.Identifier))
                return BadRequest();
            if (string.IsNullOrEmpty(checkIn.Client.ClientType?.Name))
                return BadRequest();

            if (CheckInRequest(checkIn.Client.Identifier, checkIn.Client.ClientType.Name, checkIn.ExtraData))
                return Ok();

            return BadRequest();
        }

        #region Authenticated Requests
        /// <summary>
        /// Gets all clients.
        /// </summary>
        /// <returns>The all clients.</returns>
        [HttpGet]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult GetAllClients() => Ok(_context.Client.Include(c => c.Parent).Include(c => c.ClientType).ToList());

        /// <summary>
        /// Gets all client types.
        /// </summary>
        /// <returns>The all client types.</returns>
        [HttpGet]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult GetAllClientTypes() => Ok(_context.ClientType.ToList());

        /// <summary>
        /// Gets the last 100 check ins.
        /// </summary>
        /// <returns>The last 100 check ins.</returns>
        [HttpGet]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult GetLast100CheckIns() => Ok(_context.CheckIn.Include(c => c.Client).OrderByDescending(c => c.CheckInTime).Take(100).ToList());

        /// <summary>
        /// Deletes the client.
        /// </summary>
        /// <returns>The client.</returns>
        /// <param name="clientId">Client identifier.</param>
        [HttpDelete]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult DeleteClient(int clientId)
        {
            if (clientId <= 0)
                return BadRequest();

            //get the client
            var client = _context.Client.Find(clientId);

            //if client exists, delete config associations then the client (both in DB and cache)
            if(client != null)
            {
                var configAssociations = _context.ConfigAssociation.Include(c => c.Config).Where(c => c.ClientId == clientId);
                foreach (var configAssociation in configAssociations)
                    _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, client.Identifier, client.ClientType.Name, configAssociation.Config.Key));
                _cache.Remove(string.Format(CacheKeys.CLIENT_KEY, client.Identifier, client.ClientType));
                _context.RemoveRange(configAssociations);
                _context.Remove(client);
                _context.SaveChanges();
            }

            return Ok();
        }

        /// <summary>
        /// Deletes the type of the client.
        /// </summary>
        /// <returns>The client type.</returns>
        /// <param name="clientTypeId">Client type identifier.</param>
        [HttpDelete]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult DeleteClientType(int clientTypeId)
        {
            if (clientTypeId <= 0)
                return BadRequest();

            //get the client type
            var clientType = _context.ClientType.Find(clientTypeId);

            //if the client type exists clients, client associations then the client type (both in DB and cache)
            if(clientType != null)
            {
                var clients = _context.Client.Include(c => c.ClientType).Where(c => c.ClientTypeId == clientTypeId);
                foreach(var client in clients)
                {
                    _cache.Remove(string.Format(CacheKeys.CLIENT_KEY, client.Identifier, client.ClientType.Name));
                    var configAssociations = _context.ConfigAssociation.Include(c => c.Config).Where(c => c.ClientId == client.ClientId);
                    foreach (var configAssociation in configAssociations)
                        _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, client.Identifier, client.ClientType.Name, configAssociation.Config.Key));
                    _context.RemoveRange(configAssociations);
                }
                _context.RemoveRange(clients);
                _context.Remove(clientType);
                _context.SaveChanges();
            }

            return Ok();
        }

        /// <summary>
        /// Deletes the config.
        /// </summary>
        /// <returns>The config.</returns>
        /// <param name="configId">Config identifier.</param>
        [HttpDelete]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult DeleteConfig(int configId)
        {
            if (configId <= 0)
                return BadRequest();

            //get the config
            var config = _context.Config.Find(configId);

            //if the config exists, delete all config associations than the config (both in DB and cache)
            if (config != null)
            {
                var configAssociations = _context.ConfigAssociation.Include(c => c.Client).Include(c => c.Client.ClientType).Where(c => c.ConfigId == configId);
                foreach (var configAssociation in configAssociations)
                    _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, configAssociation.Client.Identifier, configAssociation.Client.ClientType.Name, config.Key));
                _context.RemoveRange(configAssociations);
                _cache.Remove(string.Format(CacheKeys.CONFIG_KEY, config.Key));
                _context.Remove(config);
                _context.SaveChanges();
            }

            return Ok();
        }

        /// <summary>
        /// Adds the client.
        /// </summary>
        /// <returns>The client.</returns>
        /// <param name="client">Client.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult AddClient([FromBody]Client client)
        {
            if (client == null)
                return BadRequest();
            if (string.IsNullOrEmpty(client.Identifier))
                return BadRequest("Identifier cannot be blank");
            if (string.IsNullOrEmpty(client.ClientType?.Name))
                return BadRequest("Must be attached to a client type");

            var clientType = _context.ClientType.FirstOrDefault(c => c.Name == client.ClientType.Name);

            if (clientType == null)
                return BadRequest("Invalid client type");

            if (_context.Client.Any(c => c.Identifier == client.Identifier && c.ClientType.Name == client.ClientType.Name))
                return BadRequest("Client with same identifier and client type already exists");

            client = _context.Client.Add(new Client()
            {
                ClientTypeId = clientType.ClientTypeId,
                Created = DateTime.UtcNow,
                Identifier = client.Identifier,
                ParentClientId = client.ParentClientId
            }).Entity;
            _context.SaveChanges();

            //Add to cache
            _cache.Set(string.Format(CacheKeys.CLIENT_KEY, client.Identifier, clientType.Name), client, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));

            return Ok();
        }

        /// <summary>
        /// Adds the type of the client.
        /// </summary>
        /// <returns>The client type.</returns>
        /// <param name="clientType">Client type.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult AddClientType([FromBody]ClientType clientType)
        {
            if (clientType == null)
                return BadRequest();
            if (string.IsNullOrEmpty(clientType.Name))
                return BadRequest("Client Type name cannot be blank");

            if (_context.ClientType.Any(c => c.Name == clientType.Name))
                return BadRequest("Client Type already exists");

            _context.ClientType.Add(new ClientType()
            {
                Name = clientType.Name,
                Created = DateTime.UtcNow
            });
            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Adds the config.
        /// </summary>
        /// <returns>The config.</returns>
        /// <param name="config">Config.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult AddConfig([FromBody]Config config)
        {
            if (config == null)
                return BadRequest();
            if (string.IsNullOrEmpty(config.Key))
                return BadRequest("Config must have a key");
            if (string.IsNullOrEmpty(config.Value))
                return BadRequest("Config must have a value");

            if (_context.Config.Any(c => c.Key == config.Key))
                return BadRequest("A config with the same key already exists");

            config = _context.Config.Add(new Config()
            {
                Key = config.Key,
                Created = DateTime.UtcNow,
                Value = config.Value
            }).Entity;
            _context.SaveChanges();

            //Add to cache
            _cache.Set(string.Format(CacheKeys.CONFIG_KEY, config.Key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));

            return Ok();
        }

        /// <summary>
        /// Updates the client.
        /// </summary>
        /// <returns>The client.</returns>
        /// <param name="client">Client.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult UpdateClient([FromBody]Client client)
        {
            if (client == null)
                return BadRequest();
            if (string.IsNullOrEmpty(client.Identifier))
                return BadRequest("Identifier cannot be blank");
            if (string.IsNullOrEmpty(client.ClientType?.Name))
                return BadRequest("Must be attached to a client type");
            if (client.ClientId <= 0)
                return BadRequest("Client isn't created yet");

            var clientType = _context.ClientType.FirstOrDefault(c => c.Name == client.ClientType.Name);
            if (clientType == null)
                return BadRequest("Invalid client type");

            if (_context.Client.Any(c => c.Identifier == client.Identifier && c.ClientType.Name == client.ClientType.Name && c.ClientId != client.ClientId))
                return BadRequest("Client with same identifier and client type already exists");
            
            var origClient = _context.Client.AsNoTracking<Client>().Include(c => c.ClientType).FirstOrDefault(c => c.ClientId == client.ClientId);

            client = _context.Client.Update(new Client()
            {
                ClientId = client.ClientId,
                ClientTypeId = clientType.ClientTypeId,
                Identifier = client.Identifier,
                Created = origClient.Created,
                Updated = DateTime.UtcNow,
                ParentClientId = client.ParentClientId
            }).Entity;
            _context.SaveChanges();

            //update to cache
            _cache.Set(string.Format(CacheKeys.CLIENT_KEY, client.Identifier, clientType.Name), client, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            var configAssociations = _context.ConfigAssociation.Include(c => c.Config).Where(c => c.ClientId == client.ClientId);
            foreach(var configAssociation in configAssociations)
            {
                //Remove with old type/identifier
                _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, origClient.Identifier, origClient.ClientType.Name, configAssociation.Config.Key));
                //Add with new
                _cache.Set(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, client.Identifier, clientType.Name, configAssociation.Config.Key), configAssociation.Config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }

            return Ok();
        }

        /// <summary>
        /// Updates the type of the client.
        /// </summary>
        /// <returns>The client type.</returns>
        /// <param name="clientType">Client type.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult UpdateClientType([FromBody]ClientType clientType)
        {
            if (clientType == null)
                return BadRequest();
            if (string.IsNullOrEmpty(clientType.Name))
                return BadRequest("Client Type name cannot be blank");
            if (clientType.ClientTypeId <= 0)
                return BadRequest("Client Type isn't created yet");

            if (_context.ClientType.Any(c => c.Name == clientType.Name && c.ClientTypeId != clientType.ClientTypeId))
                return BadRequest("Client Type already exists");
            
            var origClientType = _context.ClientType.AsNoTracking<ClientType>().FirstOrDefault(c => c.ClientTypeId == clientType.ClientTypeId);

            _context.ClientType.Update(new ClientType()
            {
                ClientTypeId = clientType.ClientTypeId,
                Name = clientType.Name,
                Created = origClientType.Created,
                Updated = DateTime.UtcNow
            });
            _context.SaveChanges();

            //update cache
            var configAssociations = _context.ConfigAssociation.Include(c => c.Config)
                                                                .Include(c => c.Client)
                                                                .Include(c => c.Client.ClientType)
                                                                .Where(c => c.Client.ClientTypeId == clientType.ClientTypeId);
            foreach(var configAssociation in configAssociations)
            {
                //Remove with old type/identifier
                _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, configAssociation.Client.Identifier, origClientType.Name, configAssociation.Config.Key));
                //Add
                _cache.Set(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, configAssociation.Client.Identifier, clientType.Name, configAssociation.Config.Key), configAssociation.Config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }
            var clients = _context.Client.Where(c => c.ClientTypeId == clientType.ClientTypeId);
            foreach(var client in clients)
            {
                //Remove with old
                _cache.Remove(string.Format(CacheKeys.CLIENT_KEY, client.Identifier, origClientType.Name));
                //Add
                _cache.Set(string.Format(CacheKeys.CLIENT_KEY, client.Identifier, clientType.Name), client, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }

            return Ok();
        }

        /// <summary>
        /// Updates the config.
        /// </summary>
        /// <returns>The config.</returns>
        /// <param name="config">Config.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult UpdateConfig([FromBody]Config config)
        {
            if (config == null)
                return BadRequest();
            if (string.IsNullOrEmpty(config.Key))
                return BadRequest("Config must have a key");
            if (string.IsNullOrEmpty(config.Value))
                return BadRequest("Config must have a value");
            if (config.ConfigId <= 0)
                return BadRequest("Config isn't created yet");

            if (_context.Config.Any(c => c.Key == config.Key && c.ConfigId != config.ConfigId))
                return BadRequest("A config with the same key already exists");

            var origConfig = _context.Config.AsNoTracking<Config>().FirstOrDefault(c => c.ConfigId == config.ConfigId);

            config = _context.Config.Update(new Config()
            {
                ConfigId = config.ConfigId,
                Key = config.Key,
                Created = origConfig.Created,
                Updated = DateTime.UtcNow,
                Value = config.Value
            }).Entity;
            _context.SaveChanges();

            //update cache
            _cache.Set(string.Format(CacheKeys.CONFIG_KEY, config.Key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            _cache.Remove(string.Format(CacheKeys.CONFIG_KEY, origConfig.Key));
            var configAssociations = _context.ConfigAssociation.Include(c => c.Client).Include(c => c.Client.ClientType).Where(c => c.ConfigId == config.ConfigId);
            foreach(var configAssociation in configAssociations)
            {
                //remove
                _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, configAssociation.Client.Identifier, configAssociation.Client.ClientType, origConfig.Key));
                //add
                _cache.Set(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, configAssociation.Client.Identifier, configAssociation.Client.ClientType.Name, config.Key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }

            return Ok();
        }

        /// <summary>
        /// Manages the config associations for client.
        /// </summary>
        /// <returns>The config associations for client.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="configAssociations">Config associations.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult ManageConfigAssociationsForClient(int id, [FromBody]IEnumerable<ConfigAssociation> configAssociations)
        {
            if (id <= 0)
                return BadRequest("Client id is required");
            if (configAssociations == null)
                return BadRequest();
            if (configAssociations.Any(c => c.ClientId <= 0 || c.ConfigId <= 0))
                return BadRequest("Either the client or config doesn't exist");
            if (configAssociations.Select(c => c.ConfigId).Distinct().Count() != configAssociations.Count())
                return BadRequest("You cannot have duplicate configs assigned");

            var client = _context.Client.Include(c => c.ClientType).FirstOrDefault(c => c.ClientId == id);
            if (client == null)
                return BadRequest("Client does not exist");

            //Remove past configurations first (DB and cache)
            var currentConfigAssociations = _context.ConfigAssociation.AsNoTracking<ConfigAssociation>().Include(c => c.Config).Where(c => c.ClientId == client.ClientId);
            foreach (var configAssociation in currentConfigAssociations)
                _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, client.Identifier, client.ClientType.Name, configAssociation.Config.Key));
            _context.ConfigAssociation.RemoveRange(_context.ConfigAssociation.Where(c => c.ClientId == client.ClientId));

            //Add to DB and Cache
            foreach(var configAssociation in configAssociations)
            {
                var config = _context.Config.Find(configAssociation.ConfigId);
                if (config == null)
                    return BadRequest("Config does not exist");
                configAssociation.Created = DateTime.UtcNow;
                //Check default value
                if (string.IsNullOrEmpty(configAssociation.Value))
                    configAssociation.Value = config.Value;
                _context.ConfigAssociation.Add(configAssociation);
                config.Value = configAssociation.Value;
                _cache.Set(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, client.Identifier, client.ClientType.Name, config.Key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }
            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Manages the config association for config.
        /// </summary>
        /// <returns>The config association for config.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="configAssociations">Config associations.</param>
        [HttpPost]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult ManageConfigAssociationsForConfig(int id, [FromBody]IEnumerable<ConfigAssociation> configAssociations)
        {
            if (id <= 0)
                return BadRequest("Config id is required");
            if (configAssociations == null)
                return BadRequest();
            if (configAssociations.Any(c => c.ClientId <= 0 || c.ConfigId <= 0))
                return BadRequest("Either the client or config doesn't exist");
            if (configAssociations.Select(c => c.ConfigId).Distinct().Count() != configAssociations.Count())
                return BadRequest("You cannot have duplicate configs assigned");

            var config = _context.Config.Find(id);
            if (config == null)
                return BadRequest("Config does not exist");

            //Remove past configurations first (DB and cache)
            var currentConfigAssociations = _context.ConfigAssociation.AsNoTracking<ConfigAssociation>().Include(c => c.Client).Include(c => c.Client.ClientType).Where(c => c.ConfigId == config.ConfigId);
            foreach (var configAssociation in currentConfigAssociations)
                _cache.Remove(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, configAssociation.Client.Identifier, configAssociation.Client.ClientType.Name, config.Key));
            _context.ConfigAssociation.RemoveRange(_context.ConfigAssociation.Where(c => c.ConfigId == config.ConfigId));

            //Add to DB and Cache
            foreach (var configAssociation in configAssociations)
            {
                var client = _context.Client.Include(c => c.ClientType).FirstOrDefault(c => c.ClientId == configAssociation.ClientId);
                if (client == null)
                    return BadRequest("Client does not exist");
                configAssociation.Created = DateTime.UtcNow;
                //Check default value
                if (string.IsNullOrEmpty(configAssociation.Value))
                    configAssociation.Value = config.Value;
                _context.ConfigAssociation.Add(configAssociation);
                config.Value = configAssociation.Value;
                _cache.Set(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, client.Identifier, client.ClientType.Name, config.Key), config, DateTime.Now.AddMinutes(CacheKeys.CacheTimeout));
            }
            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Gets the config associations for client.
        /// </summary>
        /// <returns>The config associations for client.</returns>
        /// <param name="id">Identifier.</param>
        [HttpGet]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult GetConfigAssociationsForClient(int id)
        {
            if (id <= 0)
                return BadRequest();

            var configAssociations = _context.ConfigAssociation.Include(c => c.Config).Where(c => c.ClientId == id);

            if (configAssociations?.Any() != true)
                return NoContent();

            return Ok(configAssociations.ToList());
        }

        /// <summary>
        /// Gets the config associations for config.
        /// </summary>
        /// <returns>The config associations for config.</returns>
        /// <param name="id">Identifier.</param>
        [HttpGet]
        [Authorize(Policy = "ValidAccount")]
        public IActionResult GetConfigAssociationsForConfig(int id)
        {
            if (id <= 0)
                return BadRequest();

            var configAssociations = _context.ConfigAssociation.Include(c => c.Client).Where(c => c.ConfigId == id);

            if (configAssociations?.Any() != true)
                return NoContent();

            return Ok(configAssociations.ToList());
        }
        #endregion
    }
}
