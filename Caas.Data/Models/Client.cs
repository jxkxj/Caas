using System;
using System.Collections.Generic;

using Dapper.Contrib.Extensions;

namespace Caas.Data.Models
{
    /// <summary>
    /// Model for client configurations
    /// </summary>
    [Table("Client")]
    public class Client
    {
        /// <summary>
        /// Get or set the Id
        /// </summary>
        [Key]
        public int ClientId { get; set; }
        /// <summary>
        /// Get or set the <see cref="Client"/> identifier
        /// </summary>
        public string Identifier { get; set; }
        private ClientType _type;
        /// <summary>
        /// Get or set the <see cref="ClientType" />
        /// </summary>
        [Write(false)]
        [Computed]
        public ClientType Type
        {
            get => _type;
            set
            {
                _type = value;
                _clientTypeId = _type?.ClientTypeId ?? 0;
            }
        }
        private int _clientTypeId;
        /// <summary>
        /// Get or set the <see cref="ClientType.ClientTypeId"/>
        /// </summary>
        public int ClientTypeId
        {
            get => _clientTypeId;
            set
            {
                _clientTypeId = value;
                if (_clientTypeId != _type.ClientTypeId)
                    _type = null;
            }
        }
        /// <summary>
        /// Get or set when the <see cref="Client"/> was created
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// Get or set when the <see cref="Client"/> was updated
        /// </summary>
        public DateTime? Updated { get; set; }
        /// <summary>
        /// Get or set all <see cref="Config"/> for this <see cref="Client"/>
        /// </summary>
        [Write(false)]
        [Computed]
        public IEnumerable<Config> Configurations { get; set; }
        private Client _parent;
        /// <summary>
        /// Get or set the parent <see cref="Client"/> if applicable
        /// </summary>
        [Write(false)]
        [Computed]
        public Client Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                _parentClientId = _parent?.ClientId;
            }
        }
        private int? _parentClientId;
        /// <summary>
        /// Get or set the parent <see cref="Client.ClientId"/> if applicable
        /// </summary>
        public int? ParentClientId
        {
            get => _parentClientId;
            set
            {
                _parentClientId = value;
                if (_parentClientId != Parent?.ClientId)
                    _parent = null;
            }
        }
        /// <summary>
        /// Get or set if this top level <see cref="Client"/>
        /// </summary>
        [Write(false)]
        [Computed]
        public bool IsParent => Parent != null;
    }
}
