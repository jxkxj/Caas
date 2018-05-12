using System;
#if NETCOREAPP2_0
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#endif

using Newtonsoft.Json;

namespace Caas.Models
{
    /// <summary>
    /// Model for <see cref="Client"/> check ins
    /// </summary>
    public class CheckIn
    {
        /// <summary>
        /// Get or set the Id
        /// </summary>
#if NETCOREAPP2_0
        [Key]
#endif
        public int CheckInId { get; set; }
        private Client _client;
        /// <summary>
        /// Get or set the <see cref="Client"/>
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
        /// Get or set the <see cref="Client"/> id
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
                if (_clientId != _client?.ClientId)
                    _client = null;
            }
        }
        /// <summary>
        /// Get or set extra data for a <see cref="CheckIn"/>
        /// </summary>
        public string ExtraData { get; set; }
        /// <summary>
        /// Get or set the Check In Date and Time
        /// </summary>
#if NETCOREAPP2_0
        [Required]
#endif
        public DateTime CheckInTime { get; set; }
    }
    
    /// <summary>
    /// Model for check in with converted <see cref="ConvertedExtraData"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CheckIn<T> : CheckIn
    {
        /// <summary>
        /// Get or set the <see cref="ConvertedExtraData"/> converted to <see cref="T"/>
        /// </summary>
        [JsonIgnore]
#if NETCOREAPP2_0
        [NotMapped]
#endif
        public T ConvertedExtraData
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(ExtraData);
                }
                catch(Exception)
                {
                    return default(T);
                }
            }
            set => ExtraData = (value == null) ? null : JsonConvert.SerializeObject(value);
        }
    }
}
