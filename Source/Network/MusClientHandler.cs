using System;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms;
using TextEncoding = System.Text.Encoding;

namespace Holo.Network;

/// <summary>
/// DotNetty channel handler for MUS (Multi User Server) connections.
/// Handles housekeeping and CMS communication.
/// </summary>
public class MusClientHandler : SimpleChannelInboundHandler<IByteBuffer>
{
    private readonly string _allowedHost;

    public MusClientHandler(string allowedHost)
    {
        _allowedHost = allowedHost;
    }

    public override void ChannelActive(IChannelHandlerContext context)
    {
        string remoteIp = GetRemoteIp(context);

        // Only allow connections from the configured MUS host
        if (remoteIp != _allowedHost)
        {
            Out.WriteLine($"MUS connection rejected from unauthorized host: {remoteIp}");
            context.CloseAsync();
            return;
        }

        Out.WriteLine($"MUS connection accepted from {remoteIp}");
        base.ChannelActive(context);
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
    {
        try
        {
            byte[] data = new byte[msg.ReadableBytes];
            msg.ReadBytes(data);
            string packet = TextEncoding.ASCII.GetString(data);

            Out.WriteLine($"MUSPORTDATA: {packet}");

            ProcessMusCommand(packet);
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error processing MUS packet: {ex.Message}");
        }
        finally
        {
            // MUS connections are one-shot, close after processing
            ctx.CloseAsync();
        }
    }

    private void ProcessMusCommand(string data)
    {
        if (data.Length < 4)
            return;

        string header = data.Substring(0, 4);
        string[] parts = data.Substring(4).Split((char)2);

        try
        {
            switch (header)
            {
                case "HKTM": // Housekeeping - text message [BK]
                    {
                        int userId = int.Parse(parts[0]);
                        string message = parts[1];
                        userManager.getUser(userId)?.sendData(new HabboPacketBuilder("BK").Append(message).Build());
                        break;
                    }

                case "HKMW": // Housekeeping - alert user [mod warn]
                    {
                        int userId = int.Parse(parts[0]);
                        string message = parts[1];
                        userManager.getUser(userId)?.sendData(new HabboPacketBuilder("B!").Append(message).Separator().Build());
                        break;
                    }

                case "HKUK": // Housekeeping - kick user from room
                    {
                        int userId = int.Parse(parts[0]);
                        string message = parts[1];
                        virtualUser? user = userManager.getUser(userId);
                        if (user?.Room != null)
                            user.Room.removeUser(user.roomUser.roomUID, true, message);
                        break;
                    }

                case "HKAR": // Housekeeping - alert certain rank
                    {
                        byte rank = byte.Parse(parts[0]);
                        bool includeHigher = parts[1] == "1";
                        string message = parts[2];
                        userManager.sendToRank(rank, includeHigher, "BK" + message);
                        break;
                    }

                case "HKSB": // Housekeeping - ban user & kick from room
                    {
                        int userId = int.Parse(parts[0]);
                        string message = parts[1];
                        virtualUser? user = userManager.getUser(userId);
                        if (user != null)
                        {
                            user.sendData(new HabboPacketBuilder("@c").Append(message).Build());
                            user.Disconnect(1000);
                        }
                        break;
                    }

                case "HKRC": // Housekeeping - rehash catalogue
                    {
                        catalogueManager.Init();
                        break;
                    }

                case "UPRA": // User profile - reload appearance
                    {
                        int userId = int.Parse(parts[0]);
                        userManager.getUser(userId)?.refreshAppearance(true, true, true);
                        break;
                    }

                case "UPRC": // User profile - reload credits
                    {
                        int userId = int.Parse(parts[0]);
                        userManager.getUser(userId)?.refreshValueables(true, false);
                        break;
                    }

                case "UPRT": // User profile - reload tickets
                    {
                        int userId = int.Parse(parts[0]);
                        userManager.getUser(userId)?.refreshValueables(false, true);
                        break;
                    }

                case "UPRS": // User profile - reload subscription (and badges)
                    {
                        int userId = int.Parse(parts[0]);
                        virtualUser? user = userManager.getUser(userId);
                        if (user != null)
                        {
                            user.refreshClub();
                            user.refreshBadges();
                        }
                        break;
                    }

                case "UPRH": // User profile - reload hand
                    {
                        int userId = int.Parse(parts[0]);
                        userManager.getUser(userId)?.refreshHand("new");
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error processing MUS command {header}: {ex.Message}");
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        Out.WriteError($"MUS connection error: {exception.Message}");
        context.CloseAsync();
    }

    private static string GetRemoteIp(IChannelHandlerContext context)
    {
        var endpoint = context.Channel.RemoteAddress?.ToString() ?? "unknown";
        return endpoint.Split(':')[0];
    }
}
