using System;
#if NETCOREAPP2_0
using System.ComponentModel.DataAnnotations;
#endif

namespace Caas.Models
{
    /// <summary>
    /// Model for available type for <see cref="Client"/>
    /// </summary>
    public class ClientType
    {
        /// <summary>
        /// Get or set the id
        /// </summary>
#if NETCOREAPP2_0
        [Key]
#endif
        public int ClientTypeId { get; set; }
        /// <summary>
        /// Get or set the type name
        /// </summary>
#if NETCOREAPP2_0
        [Required]
#endif
        public string Name { get; set; }
        /// <summary>
        /// Get or set when <see cref="ClientType"/> was created
        /// </summary>
#if NETCOREAPP2_0
        [Required]
#endif
        public DateTime Created { get; set; }
        /// <summary>
        /// Get or set when <see cref="ClientType"/> was updated
        /// </summary>
        public DateTime? Updated { get; set; }
    }
}
