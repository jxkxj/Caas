using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using Caas.Models;

namespace Caas.Web.Apis
{
    /// <summary>
    /// Manage getting all <see cref="Config"/> for <see cref="Client"/>
    /// </summary>
    [Produces("application/json")]
    public class ConfigController : Controller
    {
        private readonly DatabaseContext _context;

        private readonly IMemoryCache _cache;

        /// <summary>
        /// Basic Constructor
        /// </summary>
        /// <param name="context"><see cref="DatabaseContext"/></param>
        public ConfigController(DatabaseContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
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
            //Look in cache first
            if (_cache.TryGetValue<Config>(string.Format(CacheKeys.CONFIG_IDENTIFIERTYPEKEY, identifier, type, key), out Config cacheConfig))
                return Ok(cacheConfig);

            var client = _context.Client.FirstOrDefault(c => c.Identifier == identifier && c.ClientType.Name == type);

            if (client == null)
                return NoContent();

            var config = _context.ConfigAssociation.Where(c => c.ClientId == client.ClientId && c.Config.Key == key).Select(c => c.Config);
            if (config == null)
                return NoContent();
            else
            {
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
            var client = _context.Client.FirstOrDefault(c => c.Identifier == identifier && c.ClientType.Name == type);

            if (client == null)
                return NoContent();

            var configs = _context.ConfigAssociation.Where(c => c.ClientId == client.ClientId).Select(c => c.Config);

            return Ok(configs);
        }
    }
}