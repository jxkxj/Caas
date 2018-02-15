using System;

using Dapper.Contrib.Extensions;

namespace Caas.Data.Models
{
    /// <summary>
    /// Model for the association between <see cref="Caas.Models.Config"/> and <see cref="Caas.Models.Client"/>
    /// </summary>
    [Table("ConfigAssociation")]
    public class ConfigAssociation
    {
        /// <summary>
        /// Get or set the <see cref="ConfigAssociation"/> id
        /// </summary>
        [Key]
        public int ConfigAssociationId { get; set; }
        /// <summary>
        /// Get or set the <see cref="Caas.Models.Config.ConfigId"/>
        /// </summary>
        public int ConfigId { get; set; }
        /// <summary>
        /// Get or set the <see cref="Caas.Models.Config.ConfigId"/>
        /// </summary>
        public int ClientId { get; set; }
        /// <summary>
        /// Get or set when <see cref="ConfigAssociation"/> was created
        /// </summary>
        public DateTime Created { get; set; }
    }
}
