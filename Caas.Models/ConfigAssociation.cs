using System;
#if NETCOREAPP2_0
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#endif

using Newtonsoft.Json;

namespace Caas.Models
{
    /// <summary>
    /// Model for the association between <see cref="Config"/> and <see cref="Client"/>
    /// </summary>
    public class ConfigAssociation
    {
        /// <summary>
        /// Get or set the <see cref="ConfigAssociation"/> id
        /// </summary>
#if NETCOREAPP2_0
        [Key]
#endif
        public int ConfigAssociationId { get; set; }
        private Config _config;
        /// <summary>
        /// Get or set the <see cref="Models.Config"/>
        /// </summary>
        public virtual Config Config
        {
            get => _config;
            set
            {
                _config = value;
                _configId = _config?.ConfigId ?? 0;
            }
        }
        private int _configId;
        /// <summary>
        /// Get or set the <see cref="Config.ConfigId"/>
        /// </summary>
#if NETCOREAPP2_0
        [Required]
#endif
        public int ConfigId
        {
            get => _configId;
            set
            {
                _configId = value;
                if (_configId != _config.ConfigId)
                    _config = null;
            }
        }
        private Client _client;
        /// <summary>
        /// Get or set the <see cref="Models.Client"/>
        /// </summary>
        public virtual Client Client
        {
            get => _client;
            set
            {
                _client = value;
                _clientId = _client?.ClientId ?? 0;
            }
        }
        private int _clientId;
        /// <summary>
        /// Get or set the <see cref="Config.ConfigId"/>
        /// </summary>
#if NETCOREAPP2_0
        [Required]
#endif
        public int ClientId
        {
            get => _clientId;
            set
            {
                _clientId = value;
                if (_clientId != _client.ClientId)
                    _client = null;
            }
        }
        /// <summary>
        /// Get or set when <see cref="ConfigAssociation"/> was created
        /// </summary>
#if NETCOREAPP2_0
        [Required]
#endif
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }
    }

    public class ConfigAssociation<T> : ConfigAssociation
    {
        /// <summary>
        /// Get or set the <see cref="Value"/> converted to <see cref="T"/>
        /// </summary>
        [JsonIgnore]
#if NETCOREAPP2_0
        [NotMapped]
#endif
        public T ConvertedValue
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(Value);
                }
                catch (Exception)
                {
                    return default(T);
                }
            }
            set => Value = (value == null) ? null : JsonConvert.SerializeObject(value);
        }
    }
}
