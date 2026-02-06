using System;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Holo.Virtual.Users;
using Holo.Managers;

namespace Holo.Network;

/// <summary>
/// DotNetty channel handler for game client connections.
/// Manages the lifecycle of a virtualUser and routes packets.
/// </summary>
public class GameClientHandler : ChannelHandlerAdapter
{
    private readonly int _connectionId;
    private virtualUser? _user;
    private bool _isBanned;

    public GameClientHandler(int connectionId)
    {
        _connectionId = connectionId;
    }

    public override void ChannelActive(IChannelHandlerContext context)
    {
        string remoteIp = GetRemoteIp(context);
        Out.WriteLine($"Accepted connection [{_connectionId}] from {remoteIp}");

        // Check for IP ban
        string banReason = userManager.getBanReason(remoteIp);
        if (!string.IsNullOrEmpty(banReason))
        {
            _isBanned = true;
            // Send ban message and close
            context.WriteAndFlushAsync("@c" + banReason);
            context.CloseAsync();
            return;
        }

        // Create the virtual user with the DotNetty channel
        _user = new virtualUser(_connectionId, context);

        base.ChannelActive(context);
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        if (_isBanned || _user == null)
            return;

        if (message is string packet)
        {
            try
            {
                _user.ProcessPacketAsync(packet);
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error processing packet from connection [{_connectionId}]: {ex.Message}");
            }
        }
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        Out.WriteLine($"Connection [{_connectionId}] disconnected");

        if (_user != null)
        {
            _user.Disconnect();
            _user = null;
        }

        GameSocketServer.FreeConnection(_connectionId);

        base.ChannelInactive(context);
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        Out.WriteError($"Exception on connection [{_connectionId}]: {exception.Message}");
        context.CloseAsync();
    }

    private static string GetRemoteIp(IChannelHandlerContext context)
    {
        var endpoint = context.Channel.RemoteAddress?.ToString() ?? "unknown";
        return endpoint.Split(':')[0];
    }
}
