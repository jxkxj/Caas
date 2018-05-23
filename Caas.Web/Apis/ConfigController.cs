using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

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

			var config = _context.ConfigAssociation.Where(c => c.ClientId == client.ClientId && c.Config.Key == key).Select(c => c.Config).FirstOrDefault();
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

			var configs = _context.ConfigAssociation.Where(c => c.ClientId == client.ClientId).Select(c => c.Config);

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
	}
}
