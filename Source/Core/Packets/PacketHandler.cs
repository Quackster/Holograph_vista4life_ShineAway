using System;
using Holo.Core;
using Holo.Domain.Users;

namespace Holo.Core
{
    /// <summary>
    /// Delegate for packet handler methods.
    /// </summary>
    /// <param name="user">The virtual user receiving the packet.</param>
    /// <param name="reader">The packet reader for reading packet data.</param>
    public delegate void PacketHandlerDelegate(virtualUser user, PacketReader reader);

    /// <summary>
    /// Represents a packet handler with optional login requirement.
    /// </summary>
    public class PacketHandler
    {
        /// <summary>
        /// The packet header this handler responds to (e.g., "CD", "@L").
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// The handler method to execute when this packet is received.
        /// </summary>
        public PacketHandlerDelegate Handler { get; set; }

        /// <summary>
        /// Whether this packet requires the user to be logged in.
        /// </summary>
        public bool RequiresLogin { get; set; }

        /// <summary>
        /// Initializes a new PacketHandler.
        /// </summary>
        /// <param name="header">The packet header.</param>
        /// <param name="handler">The handler method.</param>
        /// <param name="requiresLogin">Whether login is required.</param>
        public PacketHandler(string header, PacketHandlerDelegate handler, bool requiresLogin = false)
        {
            Header = header;
            Handler = handler;
            RequiresLogin = requiresLogin;
        }
    }
}
