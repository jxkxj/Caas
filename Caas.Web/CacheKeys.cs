using System;

namespace Caas.Web
{
    /// <summary>
    /// Manage Cache Keys for in-memory cache
    /// </summary>
    public static class CacheKeys
    {
        private static int _cacheTimeout = -1;
        /// <summary>
        /// Get the Cache Timeout
        /// </summary>
        public static int CacheTimeout
        {
            get
            {
		if(_cacheTimeout == -1 && (Environment.GetEnvironmentVariable("INMEMORYCACHE_USE") ?? "true") == "true")
                {
                    string timeout = Environment.GetEnvironmentVariable("INMEMORYCACHE_TIMEOUT");
                    if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out int t))
                        _cacheTimeout = t;
                    else
                        _cacheTimeout = 60; //Default
                }

                return _cacheTimeout;
            }
        }

        /// <summary>
        /// Cache key for <see cref="Models.Config"/> with Identifier, Type, and Key
        /// </summary>
        public const string CONFIG_IDENTIFIERTYPEKEY = "config_I{0}-T{1}-K{2}";

        /// <summary>
        /// Cache key for <see cref="Models.Config"/> with just Key
        /// </summary>
        public const string CONFIG_KEY = "config_K{0}";

        /// <summary>
        /// Check key for <see cref="Models.Config"/> with Identifer and Type
        /// </summary>
        public const string CLIENT_KEY = "client_I{0}-T{1}";
    }
}
