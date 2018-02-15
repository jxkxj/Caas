using System;

using Dapper.Contrib.Extensions;

namespace Caas.Data.Models
{
    /// <summary>
    /// Model for available type for <see cref="Client"/>
    /// </summary>
    [Table("ClientType")]
    public class ClientType
    {
        /// <summary>
        /// Get or set the id
        /// </summary>
        [Key]
        public int ClientTypeId { get; set; }
        /// <summary>
        /// Get or set the type name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Get or set when <see cref="ClientType"/> was created
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// Get or set when <see cref="ClientType"/> was updated
        /// </summary>
        public DateTime? Updated { get; set; }
    }
}
