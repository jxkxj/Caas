using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace Caas.Models
{
    /// <summary>
    /// Model for available configuration
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Get or set the Id
        /// </summary>
        [Key]
        public int ConfigId { get; set; }
        /// <summary>
        /// Get or set the Unique Key
        /// </summary>
        [Required]
        public string Key { get; set; }
        /// <summary>
        /// Get or set the Value
        /// </summary>
        [Required]
        public string Value { get; set; }
        /// <summary>
        /// Get or set when the <see cref="Config"/> was created
        /// </summary>
        [Required]
        public DateTime Created { get; set; }
        /// <summary>
        /// Get or set when the <see cref="Config"/> was updated
        /// </summary>
        public DateTime? Updated { get; set; }
    }
    
    /// <summary>
    /// Model for available configuration with <see cref="Type"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Config<T> : Config
    {
        /// <summary>
        /// Get or set the <see cref="Value"/> converted to <see cref="T"/>
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public T ConvertedValue
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(Value);
                }
                catch(Exception)
                {
                    return default(T);
                }
            }
            set => Value = (value == null) ? null : JsonConvert.SerializeObject(value);
        }
    }
}
