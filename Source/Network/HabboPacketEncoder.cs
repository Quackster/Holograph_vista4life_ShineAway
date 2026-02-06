using System;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using TextEncoding = System.Text.Encoding;

namespace Holo.Network;

/// <summary>
/// DotNetty encoder for outgoing Habbo packets.
/// Packets are sent as-is with a terminator byte (0x01).
/// </summary>
public class HabboPacketEncoder : MessageToByteEncoder<string>
{
    private static readonly byte PacketTerminator = 0x01;

    protected override void Encode(IChannelHandlerContext context, string message, IByteBuffer output)
    {
        // Write the packet data
        byte[] data = TextEncoding.Default.GetBytes(message);
        output.WriteBytes(data);

        // Write the terminator
        output.WriteByte(PacketTerminator);
    }
}
